using System.Collections.Generic;

namespace Smarthouse
{
    interface IModule
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        Dictionary<string, string> Description { get; set; }
        string StrongName { get; set; }//"Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        string CfgPath { get; set; }//path to config
        bool Init();//Initializing from cfg 
        bool Start();//From now it can work. If it's a daemon - starting thread
        bool Die();// Dispose all & kill all threads. Save state.
        bool ExecString();
    }
}