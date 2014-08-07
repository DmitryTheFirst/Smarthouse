using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Smarthouse.Modules
{
    public class ModuleManager
    {
        readonly List<IModule> _modules;

        #region From config
        private readonly List<RemoteSmarthouse> _smarthouses;
        private int _cfgExchangerListenPort;
        private bool _networkConfig;
        private int _connectionTimeout;
        private int _wcfPort;
        private readonly List<PluginConfig> _pluginConfigs;
        private XDocument _wcfModulesConfig;
        private bool _safeMode;
        #endregion
        IPAddress _myIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
        readonly Thread _waitSmarthousesConnects;

        public ModuleManager()
        {
            _networkConfig = false;
            _modules = new List<IModule>();
            _pluginConfigs = new List<PluginConfig>();
            _waitSmarthousesConnects = new Thread(AcceptSmarthouse);
            _smarthouses = new List<RemoteSmarthouse>();
        }
        #region Load config
        public bool LoadConfig(string cfgPath)
        {
            Console.WriteLine("Loading config: " + cfgPath);
            if (!File.Exists(cfgPath))
            {
                Console.WriteLine("Can't access/find config");
                return false;
            }
            var fullConfig = new XmlDocument();
            fullConfig.Load(cfgPath);
            _networkConfig |= LoadModuleManager(fullConfig);
            LoadPluginsConfigs(fullConfig);
            return _pluginConfigs.Any() || _modules.Any();//if no modules then we can't run smarthouse
        }
        private bool LoadModuleManager(XmlDocument fullConfig)
        {
            var smarthousesSection = fullConfig.SelectSingleNode("/config/moduleManager/smarthouses");
            bool success = true;
            #region Parse smarthouses section
            if (smarthousesSection != null)
            {
                foreach (XmlElement smarthouse in smarthousesSection.ChildNodes.OfType<XmlElement>())
                {
                    IPAddress ip;
                    int port;
                    var ipString = smarthouse.Attributes["ip"].Value;
                    var portString = smarthouse.Attributes["port"].Value;
                    if (ipString != null && portString != null &&
                        IPAddress.TryParse(ipString, out ip) && int.TryParse(portString, out port)
                        && !_smarthouses.Exists(a => a.IP.Equals(ip) && a.Port.Equals(port)))
                        _smarthouses.Add(new RemoteSmarthouse(ip, port));
                }
            }
            if (!_smarthouses.Any())
                success = false;
            #endregion
            #region cfgExchanger
            var cfgExchanger = fullConfig.SelectSingleNode("/config/moduleManager/cfgExchanger");
            if (cfgExchanger != null && cfgExchanger.Attributes != null && cfgExchanger.Attributes.Count >= 2)
            {
                var cfgExchangerPort = cfgExchanger.Attributes["port"].Value;
                var connectionTimeout = cfgExchanger.Attributes["timeoutSecs"].Value;
                if (!(int.TryParse(cfgExchangerPort, out _cfgExchangerListenPort) &&
                    int.TryParse(connectionTimeout, out _connectionTimeout)))
                    success = false;
            }
            else
                success = false;
            #endregion
            #region wcfSection
            var wcfSection = fullConfig.SelectSingleNode("/config/moduleManager/WCF");
            if (wcfSection != null && wcfSection.Attributes != null)
            {
                var wcfPortString = wcfSection.Attributes["port"].Value;
                if (!int.TryParse(wcfPortString, out  _wcfPort))
                    success = false;
            }
            #endregion
            return success;
        }
        private void LoadPluginsConfigs(XmlDocument fullConfig)
        {
            var pluginsConfigs = fullConfig.SelectSingleNode("/config/plugins");
            if (pluginsConfigs == null)
                return;
            foreach (XmlElement pluginConfig in pluginsConfigs.ChildNodes.OfType<XmlElement>())
            {
                if (pluginConfig == null) continue;

                var className = pluginConfig.Attributes["className"].Value;
                var stubClassName = pluginConfig.Attributes["stubClassName"].Value;
                Type classType = Type.GetType(className);
                Type stubClassType = Type.GetType(stubClassName);
                if (classType == null)
                {
                    Console.WriteLine(className + " doesn't exist");
                    continue;
                }
                if (stubClassType == null)
                {
                    Console.WriteLine(stubClassName + " doesn't exist");
                    continue;
                }
                Dictionary<string, string> description = new Dictionary<string, string>();
                XmlNode pluginInitCfg = pluginConfig.SelectSingleNode("moduleConfig");
                if (LoadDescription(pluginConfig, description))
                    _pluginConfigs.Add(new PluginConfig(classType, stubClassType, description, pluginInitCfg));
            }
        }
        #endregion

        public bool LoadAllModules()
        {
            #region Load all modules
            foreach (var plugin in _pluginConfigs)
            {

                if (!LoadModule(plugin.PluginClass, plugin.ModuleConfig, plugin.Description))
                {
                    if (plugin.Description.ContainsKey("name"))
                        Console.WriteLine("Error! Couldn't load: " + plugin.Description["name"]);
                    else
                        Console.WriteLine("Error! Couldn't load: " + plugin.PluginClass);
                }

                if (_modules.Last().GetType().GetInterface(typeof(IRealModule).Name, false) != null)
                    ((IRemote)_modules[_modules.Count - 1]).StubClass = plugin.PluginStubClassName;
            }
            #endregion
            #region Init all modules
            _modules.Reverse();//next cycle will be reversed, because we can delete modules. So, to save the right order of initializations, we need to reverse modules arr
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                var module = _modules[i];
                if (module.Init()) continue;//all's ok
                //ERROR
                Console.WriteLine("Error initing " + module.Description["name"] + " module");
                module.Die();
                _modules.RemoveAt(i);
            }
            #endregion
            return _pluginConfigs.Count == _modules.Count;//modules in cfg == now loaded
        }
        public void StartAllModules()
        {
            for (var i = _modules.Count - 1; i >= 0; i--) //reversed because we can delete modules from _modules
            {
                var module = _modules[i];
                if (module.GetType().GetInterface(typeof(IStubModule).Name, false) != null)
                    continue; //we are not starting stubs, because they are starting in anytime they appears
                if (module.Start()) continue;//all's ok
                //ERROR
                Console.WriteLine("Error starting " + module.Description["name"] + " module");
            }
        }

        void ReloadStubsConfig()
        {
            _wcfModulesConfig = new XDocument(
                 new XElement("smarthouse",
                              new XElement("WCF",
                                           new XAttribute("port", _wcfPort)),
                              new XElement("plugins",
                                           _modules.Where(iface => iface.GetType().GetInterface(typeof(IRealModule).Name, false) != null
                                               && ((IRemote)iface).StubClass != null).
                                           Select(a => //selecting modules where module implements IRealModule
                                                   new XElement("module",
                                                    new XAttribute("className", a.GetType()),
                                                       new XAttribute("stubClassName", ((IRemote)a).StubClass.ToString()),
                                                         new XElement("description", a.Description.Select(desc =>
                                                             new XElement("desc",
                                                                 new XAttribute("name", desc.Key),
                                                                 new XAttribute("value", desc.Value)))))))));
        }


        public bool ConnectToOtherSmarthouses()
        {
            #region Create WCF services
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior
            {
                HttpGetEnabled = true
            };
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (IRemote module in _modules.Where(a => a.GetType().GetInterface("IRemote") != null))
            {
                string serviceName = ((IModule)module).Description["name"];
                Uri baseAddress = new Uri("http://" + _myIp + ":" + _wcfPort + "/" + serviceName);
                ServiceHost host = new ServiceHost(module, baseAddress);
                host.Description.Behaviors.Add(smb);
                module.WcfHost = host;
                module.WcfHost.Open();
                Console.WriteLine("Created WCF service: " + serviceName);
            }
            #endregion

            if (!_smarthouses.Any())
            {
                return true;//no smarthouses to work with
            }

            #region Connect to smarthouses and send/recieve config
            foreach (var smarthouse in _smarthouses)
            {
                if (smarthouse.Synchronized)
                    continue; //this was already synchronized

                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(smarthouse.IP, smarthouse.Port, null, null);
                    WaitHandle wh = ar.AsyncWaitHandle;
                    try
                    {
                        if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(_connectionTimeout), false))
                        {
                            client.Close();
                            Console.WriteLine(
                                smarthouse.IP + " wasn't answering on connection request for " + _connectionTimeout + " seconds.");
                            break;
                        }

                        client.EndConnect(ar);
                        Console.WriteLine("+\t Connected to " + client.Client.RemoteEndPoint);
                        #region Exchange configs
                        string recievedConfig;
                        ReloadStubsConfig();//_wcfModulesConfig will contain all modules
                        using (NetworkStream cfgExchangeStream = client.GetStream())
                        {
                            SendConfig(cfgExchangeStream, _wcfModulesConfig.ToString());
                            recievedConfig = RecieveConfig(cfgExchangeStream);
                        }
                        #endregion

                        if (!LoadAllStubs(smarthouse.IP.ToString(), recievedConfig))
                            Console.WriteLine("Couldn't load stub " + recievedConfig);
                        else
                            smarthouse.Synchronized = true;
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Error connecting " + smarthouse.IP + ':' + smarthouse.Port);
                    }
                    finally
                    {
                        wh.Close();
                    }
                }
            }
            #endregion

            return _smarthouses.All(a => a.Synchronized);
        }

        public void StartRecievingSmarthouses(bool safeMode)
        {
            _safeMode = safeMode;
            _waitSmarthousesConnects.Start(_cfgExchangerListenPort);
        }

        public void StopRecievingSmarthouses()
        {
            _waitSmarthousesConnects.Abort();
        }

        public bool LoadModule(Type type, XmlNode cfg, Dictionary<string, string> description)
        {
            if (cfg == null) //checking cfg existance
                Console.WriteLine("Alert: config is null!");

            if (description == null || !description.ContainsKey("name"))
            {
                Console.WriteLine("Error: descripton must contain \"name\" attribute!");
                return false;//descripton cant be null. At least you need to have "name attribute"
            }

            if (ContainsModule(description["name"]))
                return false;

            if (type.GetInterface(typeof(IRealModule).Name, false) == null)
                return false;  //not implemented Module interface

            var module = (IRealModule)Activator.CreateInstance(type);
            module.Cfg = cfg;
            module.Description = description;
            module.ModuleManager = this;
            module.Dead += (a, b) => RemoveFromList("name", module.Description["name"]);
            _modules.Add(module);//adding module to list. Here works standart constructor in module
            return true;
        }
        private bool LoadAllStubs(string ip, string remoteSmarthouseConfigString)
        {
            if (string.IsNullOrWhiteSpace(remoteSmarthouseConfigString))
                return false;
            XmlDocument remoteSmarthouseConfig = new XmlDocument();
            remoteSmarthouseConfig.LoadXml(remoteSmarthouseConfigString);
            var smarthouseWcf = remoteSmarthouseConfig.SelectSingleNode("/smarthouse/WCF");
            int remoteWcfPort;
            if (smarthouseWcf == null || smarthouseWcf.Attributes == null || !int.TryParse(smarthouseWcf.Attributes["port"].Value, out remoteWcfPort))
                return false;
            var remoteWcfModules = remoteSmarthouseConfig.SelectSingleNode("/smarthouse/plugins");
            if (remoteWcfModules == null)
                return false;
            foreach (XmlElement remoteWcfModule in remoteWcfModules)
            {
                var stubType = Type.GetType(remoteWcfModule.Attributes["stubClassName"].Value);
                if (stubType == null)
                    continue;//Error
                var stub = (IStubModule)Activator.CreateInstance(stubType);
                stub.RealAddress = new IPEndPoint(IPAddress.Parse(ip), remoteWcfPort);
                stub.Description = new Dictionary<string, string>();
                LoadDescription(remoteWcfModule, stub.Description);
                if (ContainsModule(stub.Description["name"]))
                {
                    Console.WriteLine("We already have this module");
                    continue;//we can't have 2 modules with one name
                }

                if (!stub.Init())
                    continue;//Error
                if (!stub.Start())
                    continue;//Error
                stub.Dead += (a, b) => RemoveFromList("name", stub.Description["name"]);
                _modules.Add(stub);
            }
            return true;
        }

        private bool LoadDescription(XmlNode plugin, Dictionary<string, string> descriptionDictionary)
        {
            var description = plugin.SelectSingleNode("description");
            if (description == null)
                return false;
            foreach (XmlElement desc in description.ChildNodes.OfType<XmlElement>())
            {
                descriptionDictionary.Add(desc.Attributes["name"].Value, desc.Attributes["value"].Value);
            }
            return descriptionDictionary.Any();
        }

        private string RecieveConfig(NetworkStream cfgExchangeStream)
        {
            byte[] sizeBytes = new byte[sizeof(int)];
            cfgExchangeStream.Read(sizeBytes, 0, sizeof(int));
            byte[] cfgBytes = new byte[BitConverter.ToInt32(sizeBytes, 0)];//bytes count == bytes in int (usually 4)
            cfgExchangeStream.Read(cfgBytes, 0, cfgBytes.Length);
            return Encoding.UTF8.GetString(cfgBytes);
        }
        private void SendConfig(NetworkStream cfgExchangeStream, string config)
        {
            byte[] cfgBytes = Encoding.UTF8.GetBytes(config);
            byte[] sizeBytes = BitConverter.GetBytes(cfgBytes.Length);//bytes count == bytes in int (usually 4)
            cfgExchangeStream.Write(sizeBytes, 0, sizeBytes.Length);
            cfgExchangeStream.Write(cfgBytes, 0, cfgBytes.Length);
        }
        void AcceptSmarthouse(object listenerPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, (int)listenerPort); //listener for all smarthouses
            listener.Start();
            do
            {
                TcpClient client = listener.AcceptTcpClient();
                IPAddress clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                Console.WriteLine("+\t" + client.Client.RemoteEndPoint + " just connected");
                if (!(_safeMode && _smarthouses.Any(a => a.IP.Equals(clientIp))))
                {
                    Console.WriteLine("This smarthouse's IP is not in the list. Rejected " + clientIp);
                    client.Close();
                    continue;
                }

                #region Exchange configs
                using (NetworkStream cfgExchangeStream = client.GetStream())
                {
                    ReloadStubsConfig();//_wcfModulesConfig will contain all modules
                    string recievedConfig = RecieveConfig(cfgExchangeStream);
                    SendConfig(cfgExchangeStream, _wcfModulesConfig.ToString());
                    if (!LoadAllStubs(clientIp.ToString(), recievedConfig))
                        Console.WriteLine("Couldn't load stub " + recievedConfig);

                }
                #endregion
            } while (true);
        }

        public void UnloadModule(string descriptionKey, string descriptionValue)
        {
            FindModule(descriptionKey, descriptionValue).Die();
        }

        private void RemoveFromList(string descriptionKey, string descriptionValue)
        {
            _modules.Remove(_modules.First(a => a.Description[descriptionKey] == descriptionValue));
        }
        public void UnloadAllModules()
        {
            foreach (var module in _modules)
            {
                module.Die();
            }
        }


        public IModule FindModule(string key, string value)
        {
            return _modules.FirstOrDefault(a => a.Description.ContainsKey(key) && a.Description[key] == value);
        }

        public List<T> GetAllModulesByType<T>()
        {
            List<T> reslist = new List<T>();
            reslist.AddRange(_modules.
                Where(a => a.GetType().GetInterface(typeof(T).FullName) != null ||
                    string.Equals(a.GetType().BaseType.FullName, (typeof(T).FullName))).
                    Select(b => (T)b));
            return reslist;
        }

        public bool ContainsModule(string moduleName)
        {
            return _modules.Any(a => a.Description["name"] == moduleName);
        }
    }

    class RemoteSmarthouse
    {
        public RemoteSmarthouse(IPAddress ip, int port)
        {
            IP = ip;
            Port = port;
            Synchronized = false;//on add is always false
        }

        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public bool Synchronized { get; set; }
    }

    class PluginConfig
    {
        public PluginConfig(Type pluginClass, Type pluginStubClassName, Dictionary<string, string> description, XmlNode moduleConfig)
        {
            PluginClass = pluginClass;
            PluginStubClassName = pluginStubClassName;
            Description = description;
            ModuleConfig = moduleConfig;
        }

        public Type PluginClass { get; set; }
        public Type PluginStubClassName { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public XmlNode ModuleConfig { get; set; }
    }
}