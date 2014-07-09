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
            string myIp = "192.168.0.2";
            var NetworkMain = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkMain");
            var NetworkAdditional1 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional1");
            var NetworkAdditional2 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional2");

            //Thread t1 = new Thread(() => NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null));
            //Thread t2 = new Thread(() => NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null));
            //Thread t3 = new Thread(() => NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null));

            //t1.Start();
            //t2.Start();
            //t3.Start();

            NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null);
            //NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null);
            // NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null);
            byte[] big = new byte[1024*1024];

            do
            {
                Console.WriteLine(NetworkMain.SendTo("networkAdd1", big).ToString());
                //Console.WriteLine(NetworkMain.SendTo("networkAdd2", big).ToString());
                //Console.WriteLine(NetworkAdditional1.SendTo("networkMain", big).ToString());
                //Console.WriteLine(NetworkAdditional2.SendTo("networkAdd1", big).ToString());
                Thread.Sleep(100);
            } while (true);

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
