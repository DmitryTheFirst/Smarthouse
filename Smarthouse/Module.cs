using System.Collections.Generic;

namespace Smarthouse
{
    interface Module
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        Dictionary<string, HashSet<string>> description { get; set; }
        string StrongName { get; set; }//"Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        bool Init();//Initializing. If it's a daemon - starting thread
        bool Die();// Dispose all & kill all threads. Save state.
        bool ExecString();
    }
}