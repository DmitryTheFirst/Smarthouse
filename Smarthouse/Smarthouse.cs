using Smarthouse.Modules;

namespace Smarthouse
{
    class Smarthouse
    {
        public ModuleManager ModuleManager;

        public void StartSmarthouse(string moduleManagerConfigPath)
        {
            ModuleManager = new ModuleManager();
            ModuleManager.LoadAllModules(moduleManagerConfigPath);
        }



    }
}