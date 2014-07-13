using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Smarthouse
{
    class Test : IModule
    {
        private int timeout = 100;
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public XmlNode Cfg { get; set; }

        public bool Init()
        {
            return true;
        }

        private TcpNetwork NetworkMain;
        private TcpNetwork NetworkAdditional1;
        private TcpNetwork NetworkAdditional2;
        public bool Start()
        {
            NetworkMain = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkMain");
            NetworkAdditional1 = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional1");
            NetworkAdditional2 = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional2");

            Thread t1 = new Thread(Thread1);
            Thread t2 = new Thread(Thread2);
            Thread t3 = new Thread(Thread3);

            t1.Start();
            t2.Start();
            t3.Start();
            return true;
        }
        string myIp = "192.168.0.100";
        private void Thread1()
        {
            string partnerId;
            if (NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null, out partnerId))
            {
                for (int i = 1; i <= 1000; i++)
                {
                    ///Console.WriteLine(NetworkMain.SendTo(partnerId, new byte[i]));
                    Console.WriteLine(NetworkMain.SendTo("NetworkAdditional2", new byte[i]));
                    Thread.Sleep(timeout);
                }
            }
        }

        private void Thread2()
        {
            string partnerId;
            if (NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null, out partnerId))
            {
                for (int i = 1; i <= 1000; i++)
                {
                    //Console.WriteLine(NetworkAdditional1.SendTo("NetworkMain", new byte[i]));
                    //Console.WriteLine(NetworkAdditional1.SendTo(partnerId, new byte[i]));
                    Thread.Sleep(timeout);
                }
            }
        }

        private void Thread3()
        {
            string partnerId;
            if (NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null, out partnerId))
            {
                for (int i = 1; i <= 1000; i++)
                {
                    // Console.WriteLine(NetworkAdditional2.SendTo(partnerId, new byte[i]));
                    // Console.WriteLine(NetworkAdditional2.SendTo("NetworkAdditional1", new byte[i]));
                    Thread.Sleep(timeout);
                }
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
