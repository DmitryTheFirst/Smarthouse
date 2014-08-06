using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Xml;

namespace Smarthouse.Modules.Test
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Test : IRealModule, ITest, IRemote
    {
        private int timeout = 100;
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }

        public event EventHandler Dead;
        public ModuleManager ModuleManager { get; set; }
        private string myIp;
        public bool Init()
        {
            #region Parse from cfg
            myIp = Cfg.SelectSingleNode("netCfg").Attributes["ip"].Value;
            #endregion
            return true;
        }

        public int GetRandomNum(int min, int max)
        {
            return new Random().Next(min, max);
        }

        //private TcpNetwork.TcpNetwork NetworkMain;
        // private TcpNetwork.TcpNetwork NetworkAdditional1;
        // private TcpNetwork.TcpNetwork NetworkAdditional2;
        public bool Start()
        {
            Thread t1 = new Thread(Thread1);

            //NetworkMain = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkMain");
            //NetworkAdditional1 = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional1");
            //NetworkAdditional2 = (TcpNetwork)Smarthouse.moduleManager.FindModule("name", "NetworkAdditional2");

            // Thread t1 = new Thread(Thread1);
            //Thread t2 = new Thread(Thread2);
            //Thread t3 = new Thread(Thread3);

            t1.Start();
            //t2.Start();
            //t3.Start();
            return true;
        }
        private void Thread1()
        {
            //string partnerId;
            //Thread.Sleep(10000);// FIX THIS SHIT!!!!
            var otherTest = (ITest)ModuleManager.FindModule("name", "TestPi");
            if (otherTest != null)
            {

                for (int i = 0; i < 100; i++)
                    Console.WriteLine(otherTest.GetRandomNum(0, i));
            }
            else
            {
                Console.WriteLine("null");
            }
            //if (NetworkMain.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 112), null, out partnerId))
            //{
            //    Console.WriteLine("NetworkMain connected to NetworkAdditional1");
            //    for (int i = 1; i <= 1000; i++)
            //    {
            //        NetworkMain.SendTo("NetworkAdditional1", new byte[i]);
            //        NetworkMain.SendTo("NetworkAdditional2", new byte[i]);

            //        Thread.Sleep(timeout);
            //    }
            //}
        }

        private void Thread2()
        {
            string partnerId;
            //if (NetworkAdditional1.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 113), null, out partnerId))
            //{
            //    Console.WriteLine("NetworkAdditional1 connected to NetworkAdditional2");
            //    for (int i = 1; i <= 1000; i++)
            //    {
            //        //Console.WriteLine(NetworkAdditional1.SendTo("NetworkMain", new byte[i]));
            //        //Console.WriteLine(NetworkAdditional1.SendTo("NetworkAdditional2", new byte[i]));
            //        Thread.Sleep(timeout);
            //    }
            //}
        }

        private void Thread3()
        {
            string partnerId;
            //if (NetworkAdditional2.ConnectTo(new IPEndPoint(IPAddress.Parse(myIp), 111), null, out partnerId))
            //{
            //    Console.WriteLine("NetworkAdditional2 connected to NetworkMain");
            //    for (int i = 1; i <= 1000; i++)
            //    {
            //        // Console.WriteLine(NetworkAdditional2.SendTo(partnerId, new byte[i]));
            //        // Console.WriteLine(NetworkAdditional2.SendTo("NetworkAdditional1", new byte[i]));
            //        Thread.Sleep(timeout);
            //    }
            //}
        }

        public void Die()
        {
            //throw new NotImplementedException();
            if (Dead != null)
                Dead.Invoke(null, null);
        }

    }
}
