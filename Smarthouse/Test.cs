using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smarthouse
{
    class Test : IModule
    {
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }

        public bool Init()
        {
            return true;
        }

        public bool Start()
        {
            var NetworkMain = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkMain");
            var NetworkAdditional1 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional1");
            var NetworkAdditional2 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional2");
            NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse("192.168.0.3"), 112));
            NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse("192.168.0.3"), 113));
            NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse("192.168.0.3"), 111));
            Thread.Sleep(1000);
            return true;
        }

        public bool Die()
        {
            //throw new NotImplementedException();
            return true;
        }

        public bool ExecString()
        {
            throw new NotImplementedException();
        }
    }
}
