using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse
{
    class Network:Module
    {
        private Dictionary<Uri, TcpClient> connections;
        private TcpListener listener;
        Network()
        {
            connections = new Dictionary<Uri, TcpClient>();
        }

        public Dictionary<string, string> description { get; set; }
        public string StrongName { get; set; }
        public bool Init()
        {
            //listener=new TcpListener(  );//from cfg
            throw new NotImplementedException();
        }

        public void Start()
        {
            listener.Start();
            throw new NotImplementedException();
        }

        public bool Die()
        {
            throw new NotImplementedException();
        }

        public bool ExecString()
        {
            throw new NotImplementedException();
        }
    }
}
