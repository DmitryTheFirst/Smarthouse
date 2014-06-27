using System;
using System.Collections.Generic;
using System.Configuration;

namespace Smarthouse
{
    class ModuleManager
    {
        //key - strong-name(Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null), value - module
        Dictionary<string, Module> modules;

        public ModuleManager()
        {
            this.modules = new Dictionary<string, Module>();
            //create optimize dictionaries
            var plugins = (pluginsSection)ConfigurationManager.GetSection("plugins");
            for (int i = 0; i < plugins.Services.Count; i++)
                this.LoadModule(plugins.Services[i].ClassName, plugins.Services[i].ConfigPath);


        }

        public bool LoadModule(string strongName, string cfgPath)
        {
            Type type;
            try
            {
                type = Type.GetType(strongName);
            }
            catch (Exception ex)
            {
                return false;   //errors in strongName
            }

            if ( type == null )
                return false;   //can't load

            var ifaceType = typeof(Module);
            if (type.GetInterface(ifaceType.Name, false) != null)
            {
                modules.Add(strongName, (Module)Activator.CreateInstance(type));
            }



            //cfg get cfg


            return false;
        }

        public bool UnloadModule(string strongName, string cfgPath)
        {
            return this.modules[strongName].Die();
        }

        public bool UnloadAllModules()
        {
            bool success = true;
            foreach (var module in this.modules)
            {
                if (module.Value.Die() == false)
                    success = false;
            }
            return success;
        }

    }
}