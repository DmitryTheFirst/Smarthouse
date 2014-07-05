namespace Smarthouse
{
    static class Smarthouse
    {
        public static ModuleManager moduleManager;
        

        
        public static void StartSmarthouse()
        {
            moduleManager = new ModuleManager();
            moduleManager.LoadAllModules();
        }

        

    }
}