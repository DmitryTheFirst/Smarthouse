using System.Collections.Generic;
using System.Xml;

namespace Smarthouse
{
    interface IModule
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        Dictionary<string, string> Description { get; set; }
        XmlNode Cfg { get; set; }//path to config
        bool Init();//Initializing from cfg 
        bool Start();//From now it can work. If it's a daemon - starting thread
        bool Die();// Dispose all & kill all threads. Save state.
    }
}