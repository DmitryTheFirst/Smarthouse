using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Smarthouse
{
    class ModuleManager
    {
        //key - strong-name(Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null), value - module
        List<IModule> modules;

        public bool LoadAllModules()
        {
            this.modules = new List<IModule>();
            //todo create optimize dictionaries
            var plugins = (PluginsSection)ConfigurationManager.GetSection("plugins");

            #region Load all modules
            for (int i = 0; i < plugins.Services.Count; i++)
            {
                var plugin = plugins.Services[i];
                if (!LoadModule(plugin.ClassName, plugin.ConfigPath,
                    plugin.Description.OfType<NameValueConfigurationElement>().ToDictionary(a => a.Name, b => b.Value)))
                {
                    Console.WriteLine("Error! Couldn't load: " + plugins.Services[i].ClassName + ".  Cfg path: " + plugins.Services[i].ConfigPath);
                }
            }
            #endregion
            modules.Reverse();
            #region Init all modules
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                var module = modules[i];
                if (!module.Init()) //init module with it's cfg(already in module)
                {
                    Console.WriteLine("Error initing " + module.Description["name"] + " module");
                    module.Die();
                    modules.RemoveAt(i);
                }
            }
            #endregion
            #region Start all modules
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                var module = modules[i];
                if (!module.Start()) //init module with it's cfg(already in module)
                {
                    Console.WriteLine("Error starting " + module.Description["name"] + " module");
                    module.Die();
                    modules.RemoveAt(i);
                }
            }
            #endregion

            return plugins.Services.Count == modules.Count;
        }
        public bool LoadModule(string strongName, string cfgPath, Dictionary<string, string> description)
        {
            if (!File.Exists(cfgPath))//checking cfg existance
                return false;

            if (description == null)
                return false;//descripton cant be null

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
            modules[modules.Count - 1].StrongName = strongName;
            modules[modules.Count - 1].CfgPath = cfgPath;
            modules[modules.Count - 1].Description = description;

            return true;
        }
        public bool UnloadModule(string descriptionKey, string descriptionValue)
        {
            throw new NotImplementedException();
            //return this.findModule(descriptionKey, descriptionValue).Die();
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
    }
}