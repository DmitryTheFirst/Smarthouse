using System;
using System.Collections.Generic;
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
            if (crypt == null)
                cryptName = "";
            else
                cryptName = crypt.Description["name"];
            AuthClient(_tcpClient.Client, localID, cryptName);
            string cryptModuleName;
            AuthServer(_tcpClient.Client, out cryptModuleName);

            return cryptModuleName == cryptName;
        }
        private void Listener()
        {
            do
            {
                Socket _newPartner = listener.AcceptSocket(); //somebody wants to connect
                Thread auth = new Thread(
                    () =>
                    {
                        string cryptModuleName;
                        if (!AuthServer(_newPartner, out cryptModuleName))
                            return;
                        AuthClient(_newPartner, localID, cryptModuleName);
                    });
                auth.Start();
            } while (true);
        }
        public bool AuthServer(Socket newPartner, out string cryptModuleName)
        {
            byte[] lengths = new byte[2]; //length of first 
            newPartner.Receive(lengths, lengths.Length, SocketFlags.None);
            byte[] data = new byte[lengths[0] + lengths[1]]; //data
            newPartner.Receive(data, data.Length, SocketFlags.None);
            byte[] remoteIdArr = new byte[lengths[0]];
            byte[] cryptModuleArr = new byte[lengths[1]];
            Array.Copy(data, remoteIdArr, remoteIdArr.Length);
            Array.Copy(data, cryptModuleArr, cryptModuleArr.Length);
            string remoteId = Encoding.UTF8.GetString(remoteIdArr);
            string cryptModule = Encoding.UTF8.GetString(cryptModuleArr);
            cryptModuleName = cryptModule;//returning recieved module name.
            if (cryptModule != string.Empty && !Smarthouse.moduleManager.ContainsModule(cryptModule))  //if string.Empty it means that no crypt used
                return false;
            connections.Add(remoteId, new TcpPartner(newPartner, "anonymous", cryptModule));//adding new connection
            #region NEW CODE. ACHTUNG!
            //SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            //saea.Completed += Recieve;
            //newPartner.ReceiveAsync(saea);
            #endregion
            #region OLD CODE. ACHTUNG!
            //StateObject so = new StateObject();
            //so.workSocket = newPartner;
            //newPartner.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, SocketFlags.None, EndRecieve, so);
            #endregion
            Console.WriteLine("Connection from " + remoteId + "//" + Description["name"]);
            return true;
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
        #region OLD CODE. ACHTUNG!
        //class StateObject
        //{
        //    public Socket workSocket = null;
        //    public const int BUFFER_SIZE = 1024 * 1024;
        //    public byte[] buffer = new byte[BUFFER_SIZE];
        //}
        //void EndRecieve(System.IAsyncResult ar)
        //{
        //    StateObject so = (StateObject)ar.AsyncState;
        //    Console.WriteLine("Recieved: " + so.buffer.Length);
        //    so.workSocket.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, SocketFlags.None, EndRecieve, so); //вылетает ошибка при обрывании коннекта
        //}
        #region NEW CODE. ACHTUNG!
        //public void Recieve(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        //{

        //}
        #endregion

        #endregion




        private void Sender()
        {
            do
            {
                for (int i = outputBuffer.Count - 1; i >= 0; i--)
                {
                    var toSend = outputBuffer[i];
                    if (toSend == null)//sometimes occures. Was detected on 1 mb datas every millisecond
                        break;
                    var start = DateTime.Now; //todo debug
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs(); //creating data to send
                    e.SetBuffer(toSend.data, 0, toSend.data.Length);
                    if (connections[toSend.partner].Partner.SendAsync(e))
                        outputBuffer.RemoveAt(i);
                    Console.WriteLine("Time: " + (DateTime.Now - start).Milliseconds + ". To send: " + outputBuffer.Count); //todo debug
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