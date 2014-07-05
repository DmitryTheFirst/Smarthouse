using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Smarthouse
{
    class TcpNetwork : IModule, INetwork
    {
        private Dictionary<EndPoint, TcpPartner> connections;
        private TcpListener listener;
        private List<ToSend> outputBuffer;
        private int port;
        private IPAddress localIP;
        private Thread t;
        #region IModule vars
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }
        #endregion

        public TcpNetwork()
        {
            connections = new Dictionary<EndPoint, TcpPartner>();
            outputBuffer = new List<ToSend>();
            t = new Thread(new ThreadStart(Listener));
        }

        public bool Init()
        {
            #region Parse from cfg
            var root = XDocument.Load(CfgPath);
            try
            {
                if (!int.TryParse(root.Root.Element("listener").Attribute("port").Value, out port))
                    return false; //strange config
                if (!IPAddress.TryParse(root.Root.Element("listener").Attribute("localIP").Value, out localIP))
                    return false; //strange config
            }
            catch (NullReferenceException ex)
            {
                return false; //strange config
            }
            #endregion
            listener = new TcpListener(IPAddress.Any, port);//get port from cfg
            return true;
        }

        public bool Start()
        {
            listener.Start();
            t.Start();
            return true;
        }

        void Listener()
        {
            do
            {
                Socket _newPartner = listener.AcceptSocket();
                if (connections.ContainsKey(_newPartner.RemoteEndPoint))
                {
                    _newPartner.Close();
                    continue;//we already have this connection 
                }
                connections.Add(_newPartner.RemoteEndPoint, new TcpPartner(_newPartner, "anonymous"));
                Console.WriteLine("Connection from " + _newPartner.RemoteEndPoint.ToString() + "/// " + Description["name"]);
            } while (true);
        }

        public bool Die()
        {
            t.Abort();
            return true;
        }

        public bool ExecString()
        {
            throw new NotImplementedException();
        }

        public bool ConnectTo(EndPoint partnerUri)
        {
            Thread.Sleep(new Random().Next(0, 100));//todo fix!!!!! Was a problem because 2 network devices started same time and first one had no time to add new connection
            if (connections.ContainsKey(partnerUri))
                return false; //we already have this connection 
            TcpClient _tcpClient = new TcpClient();//new IPEndPoint(localIP, port) - binding TcpClient to local ip/port
            _tcpClient.Connect((IPEndPoint)partnerUri);
            connections.Add(partnerUri, new TcpPartner(_tcpClient.Client, "anonymous"));
            return true;
        }

        public bool SendTo(EndPoint partnerUri)
        {
            throw new Exception();
        }
    }

    class ToSend
    {
        public EndPoint partner { get; set; }
        public byte[] data { get; set; }
    }

    class TcpPartner
    {
        public TcpPartner(Socket partner, string username)
        {
            Partner = partner;
            Username = username;
            Crypt = null;
        }

        Socket Partner { get; set; }
        string Username { get; set; }
        private Crypt Crypt { get; set; }
    }
}
