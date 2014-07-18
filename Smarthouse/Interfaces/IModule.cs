using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Smarthouse
{
    public interface IModule
    {
        //Description.key is class name(reflection) for what i need it and value - some string to identify from callig class
        Dictionary<string, string> Description { get; set; }
        XmlNode Cfg { get; set; }//path to config
        Dictionary<string, Func<byte[]>> MethodResolver { get; set; } // stringMethodName - parserOfByte[]AndCallerOfNeededMethod
        bool Stub { get; set; }//if it's stub
        bool Init();//Initializing from cfg 
        bool Start();//From now it can work. If it's a daemon - starting thread
        void ExecSerializedCommand(string user, byte[] data);
        bool Die();// Dispose all & kill all threads. Save state.
    }
}