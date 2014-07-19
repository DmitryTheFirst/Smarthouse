namespace Smarthouse
{
    static class Smarthouse
    {
        public static ModuleManager ModuleManager;

        public static void StartSmarthouse(string moduleManagerConfigPath)
        {
            ModuleManager = new ModuleManager();
            ModuleManager.LoadAllModules(moduleManagerConfigPath);
        }



    }
}