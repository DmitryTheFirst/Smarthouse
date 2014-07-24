using System;
using Smarthouse.Modules;

namespace Smarthouse
{
    class Smarthouse
    {
        public ModuleManager ModuleManager;
        public Smarthouse()
        {
            ModuleManager = new ModuleManager();
        }
        public void EasyStart(string moduleManagerConfigPath, bool safeMode)
        {
            Console.WriteLine(
                ModuleManager.LoadConfig(moduleManagerConfigPath) ?
                "Cfg is correct" :
                "Wrong config path");
          
            Console.WriteLine(
                ModuleManager.LoadAllModules()
                    ? "All modules were loaded correctly. YAY!"
                    : "Not all modules were loaded correctly. Boo!");
            ModuleManager.StartAllModules();
            ModuleManager.StartRecievingSmarthouses(safeMode);
            Console.WriteLine(ModuleManager.ConnectToOtherSmarthouses()
                ? "All smarthouses connected"
                : "Not all smarthouses connected");
        }
    }
}