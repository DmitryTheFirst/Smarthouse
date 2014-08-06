using System;
using System.Threading;
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
            //Load cfg
            Console.WriteLine(
                ModuleManager.LoadConfig(moduleManagerConfigPath) ?
                "Cfg is correct" :
                "Wrong config path");
            //Load all real modules
            Console.WriteLine(
              ModuleManager.LoadAllModules()
                  ? "All modules were loaded correctly. YAY!"
                  : "Not all modules were loaded correctly. Boo!");
            ModuleManager.StartRecievingSmarthouses(safeMode);
            Console.WriteLine(ModuleManager.ConnectToOtherSmarthouses()
               ? "All smarthouses connected"
               : "Not all smarthouses connected");
            //Start all real modules
            ModuleManager.StartAllModules();



        }
    }
}