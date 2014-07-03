using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Smarthouse
{
    class TcpNetwork : IModule, INetwork
    {
        private Dictionary<Uri, TcpPartner> connections;
        private TcpListener listener;
        private int port;
        public TcpNetwork()
        {
            connections = new Dictionary<Uri, TcpPartner>();
        }
        #region Module
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }
        #endregion
        public bool Init()
        {
            XElement root = XElement.Load(CfgPath);
            var port = root.Elements("port");

            listener = new TcpListener(IPAddress.Any, 34);//get port from cfg
            return true;
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

        public bool ConnectTo(Uri partnerUri)
        {
            return false;
        }

        public bool SendTo(Uri partnerUri)
        {
            throw new NotImplementedException();
        }
    }

    class TcpPartner
    {
        TcpClient Partner { get; set; }
        string Username { get; set; }
        private Crypt Crypt { get; set; }
    }
}
