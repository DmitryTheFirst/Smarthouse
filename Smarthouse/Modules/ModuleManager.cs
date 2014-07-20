using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Smarthouse.Modules;
using Smarthouse.Modules.Test;

namespace Smarthouse
{

    class WCFServer
    {
        private ServiceHost host;
        public void Create(IModule module, int port)
        {

        }

        public bool Open()
        {
            try
            {
                host.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public void Close()
        {
            host.Close();
        }


    }

    class ModuleManager
    {
        List<IModule> modules;
        private string pluginsConfigPath;
        private List<RemoteSmarthouse> smarthouses;
        private XDocument smarthouseConfig;
        public bool LoadAllModules(string configPath)
        {
            string myIP = System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[0].ToString();
            pluginsConfigPath = configPath;
            modules = new List<IModule>();
            var pluginsConfig = new XmlDocument();
            pluginsConfig.Load(pluginsConfigPath);
            #region Get moduleManager configs
            var modulManagerConfig = pluginsConfig.SelectSingleNode("/config/moduleManager");//getting plugins section
            smarthouses = new List<RemoteSmarthouse>();
            var listenerPort = int.Parse(modulManagerConfig.SelectSingleNode("cfgExchanger").Attributes["port"].Value);
            var connectionTimeout = int.Parse(modulManagerConfig.SelectSingleNode("cfgExchanger").Attributes["timeoutSecs"].Value);
            var WCFport = int.Parse(modulManagerConfig.SelectSingleNode("WCF").Attributes["port"].Value);
            var smarthousesSection = modulManagerConfig.SelectSingleNode("smarthouses");
            if (smarthousesSection != null)
            {
                smarthouses.AddRange(
                    smarthousesSection.ChildNodes.OfType<XmlElement>().Cast<XmlElement>()
                                      .Select(
                                          smarthouseNode => new RemoteSmarthouse(
                                              IPAddress.Parse(smarthouseNode.Attributes["ip"].Value),
                                              int.Parse(smarthouseNode.Attributes["port"].Value))));//add smarthouses from cfg to smarthouses list
            }
            #endregion
            #region Load all modules
            var pluginSection = pluginsConfig.SelectSingleNode("/config/plugins");//getting plugins section
            foreach (XmlElement plugin in pluginSection.ChildNodes.OfType<XmlElement>())
            {
                var className = plugin.Attributes["className"];
                var moduleConfig = plugin.SelectSingleNode("moduleConfig");
                var discriptionDictionary = new Dictionary<string, string>();
                #region Filling discriptionDictionary
                foreach (XmlNode desc in plugin.SelectSingleNode("description").ChildNodes)
                {
                    if (desc.Attributes != null)
                        discriptionDictionary.Add(desc.Attributes["name"].Value, desc.Attributes["value"].Value);
                }
                #endregion
                if (LoadModule(className.Value, moduleConfig, discriptionDictionary)) continue; //all's ok

                //ERROR
                if (discriptionDictionary.ContainsKey("name"))
                    Console.WriteLine("Error! Couldn't load: " + discriptionDictionary["name"]);
                else
                    Console.WriteLine("Error! Couldn't load: " + className.Value);
            }

            #endregion
            #region Init all modules
            modules.Reverse();//next cycle will be reversed, because we can delete modules. So, to save the right order of initializations, we need to reverse modules arr
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                var module = modules[i];
                if (module.Init()) continue;//all's ok
                //ERROR
                Console.WriteLine("Error initing " + module.Description["name"] + " module");
                module.Die();
                modules.RemoveAt(i);
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
            foreach (IRemote module in modules.Where(a => a.GetType().GetInterface("IRemote") != null))
            {

                Uri baseAddress = new Uri("http://" + myIP + ":" + WCFport + "/" + ((IModule)module).Description["name"]);
                ServiceHost host = new ServiceHost(module, baseAddress);
                host.Description.Behaviors.Add(smb);
                module.WcfHost = host;
                module.WcfHost.Open();
                Console.WriteLine("Created service: " + baseAddress.ToString());
            }
            #endregion
            if (smarthouses.Count > 0)
            {
                #region Prepare xml to send
                smarthouseConfig = new XDocument(
                    new XElement("smarthouse",
                        new XElement("WCF",
                            new XAttribute("port", WCFport)
                        ),
                    new XElement("plugins", modules.Select(a =>
                            new XElement("module",
                                new XAttribute("className", a.GetType()),
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

                foreach (var smarthouse in smarthouses)
                {
                    if (smarthouse.Synchronized)
                        continue; //this was already synchronized
                    TcpClient client = new TcpClient();
                    try
                    {
                        client.Connect(smarthouse.IP.ToString(), smarthouse.Port);//Connected to the server. 
                        Console.WriteLine("+\t Connected to " + client.Client.RemoteEndPoint);


                        #region Exchange configs
                        string recievedConfig;
                        using (NetworkStream cfgExchangeStream = client.GetStream())
                        {
                            SendConfig(cfgExchangeStream, smarthouseConfig.ToString());
                            recievedConfig = RecieveConfig(cfgExchangeStream);

                        }
                        Console.WriteLine(recievedConfig);
                        var remoteSmarthouseConfig = new XmlDocument();
                        remoteSmarthouseConfig.LoadXml(recievedConfig);
                        #endregion
                        #region Creating stubs
                        int remoteWcfPort = int.Parse(remoteSmarthouseConfig.SelectSingleNode("/smarthouse/WCF").Attributes["port"].Value);
                        var remoteWcfModules = remoteSmarthouseConfig.SelectSingleNode("/smarthouse/plugins");
                        foreach (XmlElement remoteWcfModule in remoteWcfModules)
                        {
                            //var stubName = "Test";
                            //var stubInterface = Type.GetType(remoteWcfModule.Attributes["className"].Value).GetInterfaces()[2];//FIX!!!
                            //ChannelFactory<> myChannelFactory = new ChannelFactory<ITest>(new BasicHttpBinding(),
                            //                        "http://" + smarthouse.IP + ":" + remoteWcfPort + "/" + stubName
                            //    );
                            //IModule stub = myChannelFactory.CreateChannel();
                            //stub.Description=
                            //modules.Add(stub);
                        }

                        #endregion
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("-\t" + smarthouse.IP + " is not alive! " + se.Message);
                    }
                }
                #endregion
            }
            #endregion
            #region Start all modules
            for (var i = modules.Count - 1; i >= 0; i--)
            {
                var module = modules[i];
                if (module.Start()) continue;//all's ok
                //ERROR
                Console.WriteLine("Error starting " + module.Description["name"] + " module");
                module.Die();
                modules.RemoveAt(i);
            }
            #endregion
            return pluginSection.ChildNodes.Count == modules.Count;//read modules == now loaded
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
                    SendConfig(cfgExchangeStream, smarthouseConfig.ToString());
                    Console.WriteLine(recievedConfig);
                }
                #endregion
            } while (true);
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

            if (type.GetInterface(typeof(IModule).Name, false) == null)
                return false;  //not implementing Module interface

            modules.Add((IModule)Activator.CreateInstance(type));//adding module to list. Here works standart constructor in module
            modules[modules.Count - 1].Cfg = cfg;
            modules[modules.Count - 1].Description = description;
            return true;
        }
        public bool UnloadModule(string descriptionKey, string descriptionValue)
        {
            throw new NotImplementedException();
            //return findModule(descriptionKey, descriptionValue).Die();
        }
        public bool UnloadAllModules()
        {
            bool success = true;
            foreach (var module in modules)
            {
                if (module.Die() == false)
                    success = false;
            }
            return success;
        }
        public IModule FindModule(string key, string value)
        {
            return modules.FirstOrDefault(a => a.Description.ContainsKey(key) && a.Description[key] == value);
        }
        public bool ContainsModule(string moduleName)
        {
            return modules.Any(a => a.Description["name"] == moduleName);
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