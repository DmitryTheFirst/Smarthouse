using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Timer = System.Timers.Timer;

namespace Smarthouse
{
    public class TcpNetwork : INetwork
    {
        private Dictionary<string, TcpPartner> connections; // string - partner's ID
        private TcpListener listener;                       // server listener
        private List<ToSend> outputBuffer;                  // output buffer
        private int port;                                   // listened port
        private string localID;                             // current node id
        private Thread listenerThread;
        private Thread senderThread;
        private const string notIdentified = "not identified";
        private const string anonymous = "anonymous";
        private Timer sendTimer = new Timer();
        private object bufferLock = new object();
        private CancellationTokenSource listenerToken;

        #region IModule vars
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public Dictionary<string, Func<byte[]>> MethodResolver { get; set; }
        public bool Stub { get; set; }
        public EndPoint RealIp { get; set; }
        public string StubCryptModuleName { get; set; }
        public INetwork UsingNetwork { get; set; }
        public string PartnerNetworkId { get; set; }
        #endregion

        public TcpNetwork()
        {
            connections = new Dictionary<string, TcpPartner>();
            outputBuffer = new List<ToSend>();
            senderThread = new Thread(Sender);
            sendTimer.Elapsed += sendData;
        }
        public bool Init()
        {
            #region Parse from cfg
            int sendTimerInterval;
            if (!int.TryParse(Cfg.SelectSingleNode("listener").Attributes["port"].Value, out port))
                return false; //we REALLY need this port
            if (!int.TryParse(Cfg.SelectSingleNode("sender").Attributes["sendTimer"].Value, out sendTimerInterval))
                return false;
            localID = Cfg.SelectSingleNode("host").Attributes["localID"].Value;
            #endregion
            sendTimer.Interval = sendTimerInterval;
            listenerToken = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Any, port);
            return true;
        }

        private async void sendData(object sender, ElapsedEventArgs e)
        {
            ToSend[] sendData;
            lock (bufferLock)
            {
                sendData = outputBuffer.ToArray();
                outputBuffer.Clear();
            }
            var sendTasks = sendData.GroupBy(a => a.partner).Select(
                a =>
                {
                    var _a = a.Select(b => b.data).ToArray();
                    return SendToClient(_a, a.Key);
                }).ToArray();
            await Task.WhenAll(sendTasks);
        }

        private async Task SendToClient(IEnumerable<byte[]> toSends, string key)
        {
            try
            {
                var sendStream = connections[key].TcpStream;
                foreach (var t in toSends)
                {
                    var bs = BitConverter.GetBytes(t.Length);
                    await sendStream.WriteAsync(bs, 0, bs.Length);
                    await sendStream.WriteAsync(t, 0, t.Length);
                }
                await sendStream.FlushAsync();
            }
            catch (Exception ex)
            {
                ErrorHandler(ex);
            }
        }

        private void ErrorHandler(Exception exception)
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            listener.Start();
            Listener();
            sendTimer.Start();
            return true;
        }

        public void ExecSerializedCommand(string user, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool ConnectTo(EndPoint partnerUri, Crypt crypt, out string partnerId)
        {
            partnerId = null;
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
            string cryptName = crypt == null ? "" : crypt.Description["name"];
            string cryptModuleName;
            var stream = PrepareStream(_tcpClient);
            AuthClient(stream, localID, cryptName);
            AuthServer(stream, out cryptModuleName, out partnerId);
            return cryptModuleName == cryptName;
        }
        private async Task Listener()
        {
            var token = listenerToken.Token;
            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                ProcessNewConnection(client, token);
            }
        }

        private async void ProcessNewConnection(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = PrepareStream(client))
            {
                string remoteId = null;
                try
                {
                    string cryptModuleName;
                    if (!AuthServer(stream, out cryptModuleName, out remoteId)) return;
                    AuthClient(stream, localID, cryptModuleName);
                    await ProcessClient(stream, remoteId, token);
                }
                finally
                {
                    if (remoteId != null) connections.Remove(remoteId);
                }
            }
        }

        private async Task ProcessClient(Stream stream, string remoteId, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {

                byte[] lengthArr = new byte[sizeof(int)];
                stream.Read(lengthArr, 0, sizeof(int));
                var length = BitConverter.ToInt32(lengthArr, 0);
                if (length < 0) continue;
                var buf = new byte[length];
                int cnt = 0;
                do
                {
                    cnt += await stream.ReadAsync(buf, cnt, length - cnt, token);
                } while (cnt < length && !token.IsCancellationRequested);
                await ProcessData(buf, remoteId, stream);
            }
        }

        private async Task ProcessData(byte[] buff, string remoteId, Stream stream)
        {
            Console.WriteLine("Recieved " + buff.Length + " from " + remoteId);
        }

        private Stream PrepareStream(TcpClient client)
        {
            return client.GetStream();
        }

        public bool AuthServer(Stream newPartner, out string cryptModuleName, out string remoteId)
        {
            byte[] lengths = new byte[2]; //length of first 
            newPartner.Read(lengths, 0, lengths.Length);
            byte[] data = new byte[lengths[0] + lengths[1]]; //data
            newPartner.Read(data, 0, data.Length);
            byte[] remoteIdArr = new byte[lengths[0]];
            byte[] cryptModuleArr = new byte[lengths[1]];
            Array.Copy(data, remoteIdArr, remoteIdArr.Length);
            Array.Copy(data, cryptModuleArr, cryptModuleArr.Length);
            remoteId = Encoding.UTF8.GetString(remoteIdArr);
            string cryptModule = Encoding.UTF8.GetString(cryptModuleArr);
            cryptModuleName = cryptModule;//returning recieved module name.
            if (cryptModule != string.Empty && !Smarthouse.moduleManager.ContainsModule(cryptModule))  //if string.Empty it means that no crypt used
                return false;
            connections.Add(remoteId, new TcpPartner(newPartner, anonymous, cryptModule));//adding new connection
            Console.WriteLine("Connection from " + remoteId + "//" + Description["name"]);
            return true;
        }
        public void AuthClient(Stream newPartner, string localId, string cryptModule)
        {
            byte[] remoteIdArr = Encoding.UTF8.GetBytes(localId);
            byte[] cryptoModuleArr = Encoding.UTF8.GetBytes(cryptModule);
            byte[] lengths = new byte[2]; //length of first

            lengths[0] = checked((byte)remoteIdArr.Length);//remoteId must be less than 255 bytes
            lengths[1] = checked((byte)cryptoModuleArr.Length);//cryptoModule must be less than 255 bytes

            newPartner.Write(lengths, 0, lengths.Length);//sending length
            newPartner.Write(remoteIdArr, 0, remoteIdArr.Length);//sending data
            newPartner.Write(cryptoModuleArr, 0, cryptoModuleArr.Length);//sending data
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

        public bool SendTo(string partnerId, byte[] data)
        {
            if (!connections.ContainsKey(partnerId)) return false;//no such connection or it's not availiable
            lock (bufferLock)
            {
                outputBuffer.Add(new ToSend(partnerId, data));
            }
            return true;
        }

        public bool Die()
        {
            listenerToken.Cancel();
            sendTimer.Stop();
            return true;
        }
    }
}