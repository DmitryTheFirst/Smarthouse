using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;

namespace Smarthouse
{
    class ModuleManager
    {
        List<IModule> modules;
        private const string pluginsConfigPath = @"C:\Users\Smirnyaga\GoogleDrive\code\Not done\C#\Smarthouse\Smarthouse\Configs\plugins.xml";
        public bool LoadAllModules()
        {
            modules = new List<IModule>();
            XmlDocument pluginsConfig = new XmlDocument();
            pluginsConfig.Load(pluginsConfigPath);
            #region Load all modules
            var pluginSection = pluginsConfig.SelectSingleNode("/plugins");//getting plugins section
            foreach (XmlElement plugin in pluginSection.ChildNodes)
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
            modules.Reverse();//next cycle will be reversed, because we can delete modules. So, to save the right order of initializations, we need to reverse modules arr
            #region Init all modules
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
            //here we find&create create stubs

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
}