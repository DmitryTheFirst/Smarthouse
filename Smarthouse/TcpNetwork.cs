using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Smarthouse
{
    internal class TcpNetwork : IModule, INetwork
    {
        private Dictionary<string, TcpPartner> connections; // string - partner's ID
        private TcpListener listener;
        private List<ToSend> outputBuffer;
        private int port;
        private string localID;
        private Thread listenerThread;
        private Thread senderThread;
        private const string notIdentified = "not identified";
        private const string anonymous = "anonymous";

        #region IModule vars
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }
        #endregion

        public TcpNetwork()
        {
            connections = new Dictionary<string, TcpPartner>();
            outputBuffer = new List<ToSend>();
            listenerThread = new Thread(Listener);
            senderThread = new Thread(Sender);
        }
        public bool Init()
        {
            #region Parse from cfg
            var root = XDocument.Load(CfgPath);
            try
            {
                if (!int.TryParse(root.Root.Element("listener").Attribute("port").Value, out port))
                    return false; //strange config
                localID = root.Root.Element("host").Attribute("localID").Value;
            }
            catch (NullReferenceException)
            {
                return false; //strange config
            }
            #endregion

            listener = new TcpListener(IPAddress.Any, port); //get port from cfg
            return true;
        }
        public bool Start()
        {
            listener.Start();
            listenerThread.Start();
            senderThread.Start();
            return true;
        }

        public bool ConnectTo(EndPoint partnerUri)
        {
            Thread.Sleep(new Random().Next(0, 10));//todo fix!!!!! Was a problem because 2 network devices started same time and first one had no time to add new connection
            TcpClient _tcpClient = new TcpClient(); //new IPEndPoint(localIP, port) - binding TcpClient to local ip/port

            try
            {
                _tcpClient.Connect((IPEndPoint)partnerUri);
            }
            catch (SocketException)
            {
                _tcpClient.Close();
                return false; //refused connection
            }
            AuthClient(_tcpClient.Client, localID, "");
            AuthServer(_tcpClient.Client);
            return true;
        }
        private void Listener()
        {
            do
            {
                Socket _newPartner = listener.AcceptSocket(); //somebody wants to connect
                Thread auth = new Thread(
                    () =>
                    {
                        AuthServer(_newPartner);
                        AuthClient(_newPartner, localID, "");
                    });
                auth.Start();
            } while (true);
        }


        public void AuthServer(Socket newPartner)
        {
            byte[] lengths = new byte[2]; //length of first 
            newPartner.Receive(lengths, lengths.Length, SocketFlags.None);
            byte[] data = new byte[lengths[0] + lengths[1]]; //data
            newPartner.Receive(data, data.Length, SocketFlags.None);
            byte[] remoteIdArr = new byte[lengths[0]];
            byte[] cryptoModuleArr = new byte[lengths[1]];
            Array.Copy(data, remoteIdArr, remoteIdArr.Length);
            Array.Copy(data, cryptoModuleArr, cryptoModuleArr.Length);
            string remoteId = Encoding.UTF8.GetString(remoteIdArr);
            connections.Add(remoteId, new TcpPartner(newPartner, "anonymous", Encoding.UTF8.GetString(cryptoModuleArr)));

            Console.WriteLine("Connection from " + remoteId + "//" + Description["name"]);
        }
        public void AuthClient(Socket newPartner, string localId, string cryptModule)
        {
            byte[] remoteIdArr = Encoding.UTF8.GetBytes(localId);
            byte[] cryptoModuleArr = Encoding.UTF8.GetBytes(cryptModule);
            byte[] data = new byte[remoteIdArr.Length + cryptoModuleArr.Length];
            byte[] lengths = new byte[2]; //length of first 
            lengths[0] = (byte)remoteIdArr.Length;
            lengths[1] = (byte)cryptoModuleArr.Length;
            remoteIdArr.CopyTo(data, 0);
            cryptoModuleArr.CopyTo(data, remoteIdArr.Length);

            newPartner.Send(lengths);
            newPartner.Send(data);
        }

        private void Sender()
        {
            do
            {
                foreach (var toSend in outputBuffer)
                {
                    connections[toSend.partner].Partner.SendAsync(new SocketAsyncEventArgs());
                }
            } while (true);
        }
        public bool SendTo(string partnerId, byte[] data)
        {
            if (!connections.ContainsKey(partnerId) || !connections[partnerId].Partner.Connected)
                return false;//no such connection or it's not availiable
            outputBuffer.Add(new ToSend(partnerId, data));
            return true;
        }

        public void Recieve()
        {

        }

        public bool Die()
        {
            listenerThread.Abort();
            return true;
        }
    }

    internal class ToSend
    {
        public ToSend(string partner, byte[] data)
        {
            this.partner = partner;
            this.data = data;
        }

        public string partner { get; set; }
        public byte[] data { get; set; }
    }

    internal class TcpPartner
    {
        public TcpPartner(Socket partner, string username, string CryptName)
        {
            Partner = partner;
            Username = username;
            if (!String.IsNullOrWhiteSpace(CryptName))
                Crypt = (Crypt)Smarthouse.moduleManager.FindModule("name", CryptName);
        }

        public Socket Partner { get; set; }
        private string Username { get; set; }
        private Crypt Crypt { get; set; }
    }
}