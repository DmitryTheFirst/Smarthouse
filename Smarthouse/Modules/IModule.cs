using System;
using System.Collections.Generic;

namespace Smarthouse.Modules
{
    public interface IModule
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        Dictionary<string, string> Description { get; set; }

        bool Init(); //Initializing from cfg 
        bool Start(); //From now it can work. If it's a daemon - starting thread. Works in second cycle
        void Die(); // Dispose all & kill all threads. Save state. Fire event
        event EventHandler Dead; //event fires when module is already dead

    }
}