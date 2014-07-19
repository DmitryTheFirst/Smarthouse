namespace Smarthouse
{
    static class Smarthouse
    {
        public static ModuleManager moduleManager;
        
        public static void StartSmarthouse(string moduleManagerConfigPath)
        {
            moduleManager = new ModuleManager();
            moduleManager.LoadAllModules(moduleManagerConfigPath);
        }

        

    }
}