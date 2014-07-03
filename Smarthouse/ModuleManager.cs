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
        public ModuleManager()
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
            #region Init all modules
            foreach (var module in modules)
            {
                if (!module.Init()) //init module with it's cfg(already in module)
                {
                    Console.WriteLine("Error loading " + module.Description["name"] + " module");
                    module.Die();
                    modules.Remove(module);
                }
            }
            #endregion
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
            catch (Exception ex)
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
    }
}