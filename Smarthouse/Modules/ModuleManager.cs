using System;
using System.Collections.Generic;
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
    class ModuleManager
    {
        List<IModule> _modules;
        private string _pluginsConfigPath;
        private List<RemoteSmarthouse> _smarthouses;
        private XDocument _smarthouseConfig;

        public bool LoadAllModules(string configPath)
        {
            var myIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            _pluginsConfigPath = configPath;
            _modules = new List<IModule>();
            var fullConfig = new XmlDocument();
            fullConfig.Load(_pluginsConfigPath);
            #region Get moduleManager configs
            var modulManagerConfig = fullConfig.SelectSingleNode("/config/moduleManager");//getting plugins section
            _smarthouses = new List<RemoteSmarthouse>();
            if (modulManagerConfig == null)
                return false;
            var cfgExchanger = modulManagerConfig.SelectSingleNode("cfgExchanger");
            if (cfgExchanger == null || cfgExchanger.Attributes == null || cfgExchanger.Attributes.Count < 2)
                return false;
            var listenerPort = int.Parse(cfgExchanger.Attributes["port"].Value);
            var connectionTimeout = int.Parse(cfgExchanger.Attributes["timeoutSecs"].Value);
            // ReSharper disable once InconsistentNaming
            var WCFport = int.Parse(modulManagerConfig.SelectSingleNode("WCF").Attributes["port"].Value);
            var smarthousesSection = modulManagerConfig.SelectSingleNode("smarthouses");
            if (smarthousesSection != null)
            {
                _smarthouses.AddRange(
                    smarthousesSection.ChildNodes.OfType<XmlElement>()
                                      .Select(
                                          smarthouseNode => new RemoteSmarthouse(
                                              IPAddress.Parse(smarthouseNode.Attributes["ip"].Value),
                                              int.Parse(smarthouseNode.Attributes["port"].Value))));//add smarthouses from cfg to smarthouses list
            }
            #endregion
            var pluginSection = fullConfig.SelectSingleNode("/config/plugins");//getting plugins section
            if (pluginSection == null) return true;
            #region Load all modules
            foreach (XmlElement plugin in pluginSection.ChildNodes.OfType<XmlElement>())
            {
                var className = plugin.Attributes["className"];
                var moduleConfig = plugin.SelectSingleNode("moduleConfig");
                var discriptionDictionary = new Dictionary<string, string>();
                LoadDescription(plugin, discriptionDictionary);
                if (!LoadModule(className.Value, moduleConfig, discriptionDictionary))
                {
                    if (discriptionDictionary.ContainsKey("name"))
                        Console.WriteLine("Error! Couldn't load: " + discriptionDictionary["name"]);
                    else
                        Console.WriteLine("Error! Couldn't load: " + className.Value);
                }

                if (_modules.Last().GetType().GetInterface(typeof(IRealModule).Name, false) != null)
                    ((IRemote)_modules[_modules.Count - 1]).StubClassName = plugin.Attributes["stubClassName"].Value;

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
            #region Working with stubs
            #region Create WCF services
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior
            {
                HttpGetEnabled = true,
                MetadataExporter =
                {
                    PolicyVersion = PolicyVersion.Policy15
                }
            };
            foreach (IRemote module in _modules.Where(a => a.GetType().GetInterface("IRemote") != null))
            {

                Uri baseAddress = new Uri("http://" + myIp + ":" + WCFport + "/" + ((IModule)module).Description["name"]);
                ServiceHost host = new ServiceHost(module, baseAddress);
                host.Description.Behaviors.Add(smb);
                module.WcfHost = host;
                module.WcfHost.Open();
                Console.WriteLine("Created service: " + baseAddress);
            }
            #endregion

            if (_smarthouses.Count > 0)
            {
                #region Prepare xml to send
                _smarthouseConfig = new XDocument(
                    new XElement("smarthouse",
                                 new XElement("WCF",
                                              new XAttribute("port", WCFport)),
                                 new XElement("plugins",
                                              _modules.Where(iface => iface.GetType().GetInterface(typeof(IRealModule).Name, false) != null).Select(a => //selecting modules where module implements IRealModule
                                                                                                                                                    new XElement("module",
                                                                                                                                                                 new XAttribute("className", a.GetType()),
                                                                                                                                                                 new XAttribute("stubClassName", ((IRemote)a).StubClassName),
                                                                                                                                                                 new XElement("description", a.Description.Select(desc =>
                                                                                                                                                                                                                  new XElement("desc",
                                                                                                                                                                                                                               new XAttribute("name", desc.Key),
                                                                                                                                                                                                                               new XAttribute("value", desc.Value)
                                                                                                                                                                                                                      )
                                                                                                                                                                                                 )
                                                                                                                                                                     )

                                                                                                                                                        )
                                                  )
                                     )
                        ));
                #endregion
                #region Connect to smarthouses and send/recieve config
                Thread waitSmarthousesConnects = new Thread(AcceptSmarthouse);
                waitSmarthousesConnects.Start(listenerPort);

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
                            if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(connectionTimeout), false))
                            {
                                client.Close();
                                Console.WriteLine(
                                    smarthouse.IP + " wasn't answering on connection request for " + connectionTimeout + " seconds.");
                                break;
                            }

                            client.EndConnect(ar);
                            Console.WriteLine("+\t Connected to " + client.Client.RemoteEndPoint);
                            #region Exchange configs
                            string recievedConfig;
                            using (NetworkStream cfgExchangeStream = client.GetStream())
                            {
                                SendConfig(cfgExchangeStream, _smarthouseConfig.ToString());
                                recievedConfig = RecieveConfig(cfgExchangeStream);

                            }
                            Console.WriteLine(recievedConfig);
                            #endregion
                            LoadStub(smarthouse.IP.ToString(), recievedConfig);
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine("Error connecting " + smarthouse.IP + ':' + smarthouse.Port + "\n" + se.Message);
                        }
                        finally
                        {
                            wh.Close();
                        }


                    }


                }
                #endregion
            }
            #endregion
            #region Start all modules
            for (var i = _modules.Count - 1; i >= 0; i--)
            {
                var module = _modules[i];


                if (module.GetType().GetInterface(typeof(IStubModule).Name, false) != null)
                    continue; //we are not starting stubs, because they are starting in anytime they appears


                if (module.Start()) continue;//all's ok
                //ERROR
                Console.WriteLine("Error starting " + module.Description["name"] + " module");
                module.Die();
                _modules.RemoveAt(i);
            }
            #endregion
            return pluginSection.ChildNodes.Count == _modules.Count;//read modules == now loaded
        }

        public bool LoadModule(string strongName, XmlNode cfg, Dictionary<string, string> description)
        {
            if (cfg == null) //checking cfg existance
                Console.WriteLine("Allert: config is null!");

            if (description == null || !description.ContainsKey("name"))
            {
                Console.WriteLine("Error: descripton must contain \"name\" attribute!");
                return false;//descripton cant be null. At least you need to have "name attribute"
            }

            Type type;
            try
            {
                type = Type.GetType(strongName);
            }
            catch (Exception)
            {
                return false;   //errors in strongName
            }

            if (type == null)
                return false;   //can't load

            if (type.GetInterface(typeof(IRealModule).Name, false) == null)
                return false;  //not implementing Module interface

            var module = (IRealModule)Activator.CreateInstance(type);
            module.Cfg = cfg;
            module.Description = description;
            module.Dead += (a, b) => RemoveFromList("name", module.Description["name"]);
            _modules.Add(module);//adding module to list. Here works standart constructor in module
            return true;
        }
        private void LoadStub(string ip, string remoteSmarthouseConfigString)
        {
            XmlDocument remoteSmarthouseConfig = new XmlDocument();
            remoteSmarthouseConfig.LoadXml(remoteSmarthouseConfigString);
            int remoteWcfPort = int.Parse(remoteSmarthouseConfig.SelectSingleNode("/smarthouse/WCF").Attributes["port"].Value);
            var remoteWcfModules = remoteSmarthouseConfig.SelectSingleNode("/smarthouse/plugins");
            foreach (XmlElement remoteWcfModule in remoteWcfModules)
            {
                var stubType = Type.GetType(remoteWcfModule.Attributes["stubClassName"].Value);
                var stub = (IStubModule)Activator.CreateInstance(stubType);
                stub.RealAddress = new IPEndPoint(IPAddress.Parse(ip), remoteWcfPort);
                stub.Description = new Dictionary<string, string>();
                LoadDescription(remoteWcfModule, stub.Description);
                if (!stub.Init())
                    continue;//Error
                if (!stub.Start())
                    continue;//Error
                stub.Dead += (a, b) => RemoveFromList("name", stub.Description["name"]);
                _modules.Add(stub);
            }

        }

        private static void LoadDescription(XmlNode plugin, Dictionary<string, string> discriptionDictionary)
        {
            foreach (XmlNode desc in plugin.SelectSingleNode("description").ChildNodes)
            {
                if (desc.Attributes != null)
                    discriptionDictionary.Add(desc.Attributes["name"].Value, desc.Attributes["value"].Value);
            }
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
                Console.WriteLine("+\t" + client.Client.RemoteEndPoint + " just connected");
                #region Exchange configs
                using (NetworkStream cfgExchangeStream = client.GetStream())
                {
                    string recievedConfig = RecieveConfig(cfgExchangeStream);
                    SendConfig(cfgExchangeStream, _smarthouseConfig.ToString());
                    Console.WriteLine(recievedConfig);
                    LoadStub(
                        ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()
                        , recievedConfig);

                }
                #endregion
            } while (true);
        }

        public bool UnloadModule(string descriptionKey, string descriptionValue)
        {
            throw new NotImplementedException();
            //return findModule(descriptionKey, descriptionValue).Die();
        }

        private void RemoveFromList(string descriptionKey, string descriptionValue)
        {
            
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
}