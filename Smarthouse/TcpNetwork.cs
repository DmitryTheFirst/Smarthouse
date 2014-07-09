using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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

        public bool ConnectTo(EndPoint partnerUri, Crypt crypt)
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
            string cryptName;
            cryptName = crypt == null ? "" : crypt.Description["name"];
            AuthClient(_tcpClient.GetStream(), localID, cryptName);
            string cryptModuleName;
            AuthServer(_tcpClient.GetStream(), out cryptModuleName);
            return cryptModuleName == cryptName;
        }
        private void Listener()
        {
            do
            {
                TcpClient _newPartner = listener.AcceptTcpClient(); //somebody wants to connect
                Thread auth = new Thread(
                    () =>
                    {
                        string cryptModuleName;
                        NetworkStream partnerStream = _newPartner.GetStream();
                        if (!AuthServer(partnerStream, out cryptModuleName))
                            return;
                        AuthClient(partnerStream, localID, cryptModuleName);
                        byte[] readBuff = new byte[sizeof(int)];
                        partnerStream.BeginRead(readBuff, 0, sizeof(int), onSizeRecieve, partnerStream); //starting recieving data
                    });
                auth.Start();
            } while (true);
        }

        class StateObject
        {
            private byte[] buff = new byte[sizeof(int)];
            private NetworkStream partnerStream { get; set; }
        }

        public bool AuthServer(NetworkStream newPartner, out string cryptModuleName)
        {
            byte[] lengths = new byte[2]; //length of first 
            newPartner.Read(lengths, 0, lengths.Length);
            byte[] data = new byte[lengths[0] + lengths[1]]; //data
            newPartner.Read(data, 0, data.Length);
            byte[] remoteIdArr = new byte[lengths[0]];
            byte[] cryptModuleArr = new byte[lengths[1]];
            Array.Copy(data, remoteIdArr, remoteIdArr.Length);
            Array.Copy(data, cryptModuleArr, cryptModuleArr.Length);
            string remoteId = Encoding.UTF8.GetString(remoteIdArr);
            string cryptModule = Encoding.UTF8.GetString(cryptModuleArr);
            cryptModuleName = cryptModule;//returning recieved module name.
            if (cryptModule != string.Empty && !Smarthouse.moduleManager.ContainsModule(cryptModule))  //if string.Empty it means that no crypt used
                return false;
            connections.Add(remoteId, new TcpPartner(newPartner, anonymous, cryptModule));//adding new connection
            //
            Console.WriteLine("Connection from " + remoteId + "//" + Description["name"]);
            return true;
        }
        public void AuthClient(NetworkStream newPartner, string localId, string cryptModule)
        {
            byte[] remoteIdArr = Encoding.UTF8.GetBytes(localId);
            byte[] cryptoModuleArr = Encoding.UTF8.GetBytes(cryptModule);
            byte[] data = new byte[remoteIdArr.Length + cryptoModuleArr.Length];
            byte[] lengths = new byte[2]; //length of first 
            lengths[0] = (byte)remoteIdArr.Length;//remoteId must be less than 255 bytes
            lengths[1] = (byte)cryptoModuleArr.Length;//cryptoModule must be less than 255 bytes
            remoteIdArr.CopyTo(data, 0);
            cryptoModuleArr.CopyTo(data, remoteIdArr.Length);

            newPartner.Write(lengths, 0, lengths.Length);//sending length
            newPartner.Write(remoteIdArr, 0, remoteIdArr.Length);//sending data
        }

        private void Sender()
        {
            do
            {
                for (int i = outputBuffer.Count - 1; i >= 0; i--)
                {
                    var toSend = outputBuffer[i];
                    if (toSend == null)//sometimes occures. Was detected on 1 mb datas every millisecond
                        break;
                    byte[] sizeArr = BitConverter.GetBytes(toSend.data.Length);//4 bytes for int
                    var start = DateTime.Now; //todo debug
                    var partnerStream = connections[toSend.partner].TcpStream;
                    partnerStream.BeginWrite(sizeArr, 0, sizeArr.Length, null, null);
                    partnerStream.BeginWrite(toSend.data, 0, toSend.data.Length, null, null);
                    outputBuffer.RemoveAt(i);
                    Console.WriteLine("Time: " + (DateTime.Now - start).Milliseconds + ". To send: " + outputBuffer.Count); //todo debug
                }
            } while (true);
        }

        private void onSizeRecieve(IAsyncResult ar)
        {
            NetworkStream partnerStream = (NetworkStream)ar.AsyncState;
            byte[] size = new byte[4];

            partnerStream.BeginRead(new byte[sizeof(int)], 0, sizeof(int), onSizeRecieve, null);//continuing  recieving data
        }

        public bool SendTo(string partnerId, byte[] data)
        {
            if (!connections.ContainsKey(partnerId))
                return false;//no such connection or it's not availiable
            outputBuffer.Add(new ToSend(partnerId, data));
            return true;
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
        public TcpPartner(NetworkStream partnerStream, string username, string CryptName)
        {
            TcpStream = partnerStream;
            Username = username;
            if (!String.IsNullOrWhiteSpace(CryptName))
                Crypt = (Crypt)Smarthouse.moduleManager.FindModule("name", CryptName);
        }

        public NetworkStream TcpStream { get; set; }
        private string Username { get; set; }
        private Crypt Crypt { get; set; }
    }
}