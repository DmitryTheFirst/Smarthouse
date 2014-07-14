using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace Smarthouse
{
    class ModuleManager
    {
        List<IModule> modules;
        private string pluginsConfigPath;
        public bool LoadAllModules(string configPath)
        {
            pluginsConfigPath = configPath;
            modules = new List<IModule>();
            var pluginsConfig = new XmlDocument();
            pluginsConfig.Load(pluginsConfigPath);
            #region Get moduleManager configs
            var modulManagerConfig = pluginsConfig.SelectSingleNode("/config/moduleManager");//getting plugins section
            var smarthouses = new List<RemoteSmarthouse>();
            var listenerPort = int.Parse(modulManagerConfig.SelectSingleNode("listener").Attributes["port"].Value);
            var smarthousesSection = modulManagerConfig.SelectSingleNode("smarthouses");
            if (smarthousesSection != null)
            {
                smarthouses.AddRange(
                    smarthousesSection.ChildNodes.Cast<XmlElement>()
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
            TcpListener listener = new TcpListener(IPAddress.Any, listenerPort); //listener for all smarthouses
            listener.Start();
            listener.BeginAcceptTcpClient(AcceptSmarthouse, listener);
            for (int i = 0; i < smarthouses.Count; i++)
            {
                var smarthouse = smarthouses[i];
                if (smarthouse.Synchronized)
                    continue;  //this was already synchronized
                TcpClient client = new TcpClient();
                client.BeginConnect(smarthouse.IP, smarthouse.Port, ConnectSmarthouse, client);
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

        #region Working with stubs
        void AcceptSmarthouse(IAsyncResult ar)
        {
            Console.WriteLine("SERVER AcceptSmarthouse");

        }
        void ConnectSmarthouse(IAsyncResult ar)
        {
            Console.WriteLine("CLIENT ConnectSmarthouse");
        }
        #endregion
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