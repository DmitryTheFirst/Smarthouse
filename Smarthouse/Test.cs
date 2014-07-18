using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Xml;

namespace Smarthouse
{
    class Test : IModule
    {
        private int timeout = 100;
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public Dictionary<string, Func<byte[]>> MethodResolver { get; set; }
        public bool Stub { get; set; }
        public EndPoint RealIp { get; set; }
        public string StubCryptModuleName { get; set; }
        public TcpNetwork UsingNetwork { get; set; }
        public string PartnerNetworkId { get; set; }
        private string myIp;
        public bool Init()
        {
            #region Parse from cfg
            myIp = Cfg.SelectSingleNode("netCfg").Attributes["ip"].Value;
            #endregion
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

            //t1.Start();
            //t2.Start();
            //t3.Start();
            return true;
        }

        public void ExecSerializedCommand(string user, byte[] data)
        {
            throw new NotImplementedException();
        }


        private void Thread1()
        {
            string partnerId;
            if (NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null, out partnerId))
            {
                Console.WriteLine("NetworkMain connected to NetworkAdditional1");
                for (int i = 1; i <= 1000; i++)
                {
                    NetworkMain.SendTo("NetworkAdditional1", new byte[i]);
                    NetworkMain.SendTo("NetworkAdditional2", new byte[i]);

                    Thread.Sleep(timeout);
                }
            }
        }

        private void Thread2()
        {
            string partnerId;
            if (NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null, out partnerId))
            {
                Console.WriteLine("NetworkAdditional1 connected to NetworkAdditional2");
                for (int i = 1; i <= 1000; i++)
                {
                    //Console.WriteLine(NetworkAdditional1.SendTo("NetworkMain", new byte[i]));
                    //Console.WriteLine(NetworkAdditional1.SendTo("NetworkAdditional2", new byte[i]));
                    Thread.Sleep(timeout);
                }
            }
        }

        private void Thread3()
        {
            string partnerId;
            if (NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null, out partnerId))
            {
                Console.WriteLine("NetworkAdditional2 connected to NetworkMain");
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
