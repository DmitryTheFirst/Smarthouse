using System.Collections.Generic;

namespace Smarthouse
{
    internal abstract class Module
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        public Dictionary<string, HashSet<string>> description;
        public string StrongName;//"Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        public abstract bool Init();//Initializing. If it's a daemon - starting thread
        public abstract bool Die();// Dispose all & kill all threads. Save state.
        public abstract bool ExecString();
    }
}