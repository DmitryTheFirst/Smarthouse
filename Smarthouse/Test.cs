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
        private int timeout = 1;
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }

        public bool Init()
        {
            return true;
        }

        private INetwork NetworkMain;
        private INetwork NetworkAdditional1;
        private INetwork NetworkAdditional2;
        public bool Start()
        {
           // NetworkMain = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkMain");
            //NetworkAdditional1 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional1");
            //NetworkAdditional2 = (INetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional2");
            
           // Thread t1 = new Thread(Thread1);
           // Thread t2 = new Thread(Thread2);
           // Thread t3 = new Thread(Thread3);
            
           // t1.Start();
           // t2.Start();
           // t3.Start();
            //Console.ReadLine();
            return true;
        }
        string myIp = "192.168.0.5";
        private void Thread1()
        {
            NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null);
            for (int i = 1; i <= 1000; i++)
            {
                NetworkMain.SendTo("networkAdd1", new byte[i]);
                NetworkMain.SendTo("networkAdd2", new byte[i]);
                Thread.Sleep(timeout);
            }
        }

        private void Thread2()
        {
            NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null);
            for (int i = 1; i <= 1000; i++)
            {
                NetworkAdditional1.SendTo("networkMain", new byte[i]);
                NetworkAdditional1.SendTo("networkAdd2", new byte[i]);
                Thread.Sleep(timeout);
            }
        }

        private void Thread3()
        {
            NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null);
            for (int i = 1; i <= 1000; i++)
            {
                NetworkAdditional2.SendTo("networkMain", new byte[i]);
                NetworkAdditional2.SendTo("networkAdd1", new byte[i]);
                Thread.Sleep(timeout);
            }
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
