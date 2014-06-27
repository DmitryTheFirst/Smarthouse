using System.Collections.Generic;

namespace Smarthouse
{
    class ModuleManager
    {
        //key - strong-name(Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null), value - module
        Dictionary<string, Module> modules;

        public ModuleManager(string pathToCfgDir)
        {
            this.modules = new Dictionary<string, Module>();
            //create optimize dictionaries
            //chec pathToCfgDir
            //load all cfg's
        }

        public bool LoadModule(string cfg)
        {
            return false;
        }

        public bool UnloadModule(string strongName)
        {
            return this.modules[strongName].Die();
        }

        public bool UnloadAllModules()
        {
            bool success = true;
            foreach ( var module in this.modules )
            {
                if ( module.Value.Die() == false )
                    success = false;
            }
            return success;
        }

    }
}