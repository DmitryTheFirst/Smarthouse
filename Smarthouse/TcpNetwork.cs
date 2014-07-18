using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Timer = System.Timers.Timer;

namespace Smarthouse
{
    public class TcpNetwork : INetwork
    {
        #region IModule
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public Dictionary<string, Func<byte[]>> MethodResolver { get; set; }
        public bool Stub { get; set; }
        #endregion

        private const string AnonymousName = "anonymous";
        private readonly object _pipesLocker = new object();

        private readonly object _sendBufferLocker = new object();
        private NetworkConfig _configuration;

        private TcpListener _listener;
        private CancellationTokenSource _cancelToken;
        private Dictionary<string, TcpPartner> _pipes;
        private List<Tuple<string, byte[]>> _sendBuffer;
        private Timer _sendTimer;

        #region INetwork
        public bool Init()
        {
            if (!LoadConfigInternal()) return false;

            _sendBuffer = new List<Tuple<string, byte[]>>();
            _pipes = new Dictionary<string, TcpPartner>();
            _listener = new TcpListener(IPAddress.Any, _configuration.Port);
            _cancelToken = new CancellationTokenSource();
            _sendTimer = new Timer(_configuration.FlushInterval);
            _sendTimer.Elapsed += SendHanler;
            return true;
        }

        public bool Start()
        {
            _listener.Start();
            Listener();
            _sendTimer.Start();
            return true;
        }

        public void ExecSerializedCommand(string user, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool Die()
        {
            _sendTimer.Stop();
            _listener.Stop();
            _cancelToken.Cancel();
            return true;
        }

        private bool AuthServer(Stream newPartner, out string cryptModuleName, out string remoteId)
        {
            var wstream = new BinaryReader(newPartner);
            var cryptNameLength = wstream.ReadByte();
            var idLength = wstream.ReadByte();
            cryptModuleName = Encoding.UTF8.GetString(wstream.ReadBytes(cryptNameLength));
            remoteId = Encoding.UTF8.GetString(wstream.ReadBytes(idLength));
            return String.IsNullOrEmpty(cryptModuleName) || Smarthouse.moduleManager.ContainsModule(cryptModuleName);
        }

        private void AuthClient(Stream newPartner, string localId, string cryptModule)
        {
            var remoteIdArr = Encoding.UTF8.GetBytes(localId);
            var cryptoModuleArr = Encoding.UTF8.GetBytes(cryptModule);

            var wstream = new BinaryWriter(newPartner);
            wstream.Write(checked((byte)cryptoModuleArr.Length)); //cryptoModule must be less than 255 bytes
            wstream.Write(checked((byte)remoteIdArr.Length)); //remoteId must be less than 255 bytes
            newPartner.Write(cryptoModuleArr, 0, cryptoModuleArr.Length); //sending data
            newPartner.Write(remoteIdArr, 0, remoteIdArr.Length); //sending data

        }

        public bool ConnectTo(EndPoint partnerUri, Crypt crypt, out string partnerId)
        {
            bool pipeOk = false;
            TcpClient tc = null;
            partnerId = null;
            try
            {
                tc = new TcpClient();
                tc.Connect((IPEndPoint)partnerUri);
                pipeOk = ClientProcessPipe(tc, crypt, out partnerId);
            }
            catch { }
            finally
            {
                if (tc != null)
                    tc.Close();
            }
            return pipeOk;
        }

        public bool SendTo(string partnerId, byte[] data)
        {
            if (!_pipes.ContainsKey(partnerId)) return false;//no such connection or it's not availiable
            lock (_sendBufferLocker) { _sendBuffer.Add(new Tuple<string, byte[]>(partnerId, data)); }
            return true;
        }
        #endregion

        private async Task Listener()
        {
            while (true)
                ServerProcessPipe(await _listener.AcceptTcpClientAsync());
        }

        private bool LoadConfigInternal()
        {
            try
            {
                var cfg = new NetworkConfig();
                int tmp;
                if (!int.TryParse(Cfg["listener"].Attributes["port"].Value, out tmp))
                    return false;
                cfg.Port = tmp;
                if (!int.TryParse(Cfg["sender"].Attributes["sendTimer"].Value, out tmp))
                    return false;
                cfg.FlushInterval = tmp;
                cfg.LocalId = Cfg["host"].Attributes["localID"].Value;
                _configuration = cfg;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Pipe management

        /// <summary>
        ///     Add new pipe
        /// </summary>
        /// <param name="remoteId">Pipe id</param>
        /// <param name="cryptName">Cryptomodule name</param>
        /// <param name="stream">Pipe stream</param>
        protected void AddPipe(string remoteId, string cryptName, Stream stream)
        {
            if (String.IsNullOrEmpty(remoteId))
                throw new ArgumentNullException("remoteId");
            var partner = new TcpPartner(stream, AnonymousName, cryptName);
            lock (_pipesLocker)
            {
                _pipes.Add(remoteId, partner);
            }
            ReadClient(partner, remoteId);
        }

        /// <summary>
        ///     Get wrapped stream for client
        /// </summary>
        /// <param name="client">Client</param>
        /// <returns>Wrapped stream</returns>
        private Stream GetPipeStream(TcpClient client)
        {
            //var stream = new BufferedStream(client.GetStream(), 65536);
            //return stream;
            return client.GetStream();
        }

        /// <summary>
        ///     Process new pipe
        /// </summary>
        /// <param name="client">TcpClient to partner</param>
        private void ServerProcessPipe(TcpClient client)
        {
            var stream = GetPipeStream(client);
            string remoteId = null;
            var pipeOk = false;
            try
            {
                string cryptname;
                if (!AuthServer(stream, out cryptname, out remoteId)) return;
                AuthClient(stream, _configuration.LocalId, cryptname);
                AddPipe(remoteId, cryptname, stream);
                pipeOk = true;
            }
            finally
            {
                if (!pipeOk)
                    RemovePipe(remoteId);
            }
        }


        private bool ClientProcessPipe(TcpClient tc, Crypt crypt, out string partnerId)
        {
            var stream = GetPipeStream(tc);
            partnerId = null;
            var pipeOk = false;
            try
            {
                string cryptname2;
                var cryptname1 = crypt == null ? "" : crypt.Description["name"]; ;

                AuthClient(stream, _configuration.LocalId, cryptname1);
                if (!AuthServer(stream, out cryptname2, out partnerId)) return false;

                if (cryptname1 != cryptname2) return false;
                AddPipe(partnerId, cryptname1, stream);
                pipeOk = true;
            }
            finally
            {
                if (!pipeOk)
                    RemovePipe(partnerId);
            }
            return pipeOk;
        }
        /// <summary>
        ///     Remove & dispose pipe
        /// </summary>
        /// <param name="remoteId"></param>
        protected void RemovePipe(string remoteId)
        {
            if (String.IsNullOrEmpty(remoteId)) return;
            lock (_pipesLocker)
            {
                TcpPartner cs;
                if (!_pipes.TryGetValue(remoteId, out cs)) return;
                if (cs != null && cs.TcpStream != null)
                {
                    try
                    {
                        cs.Dispose();
                    }
                    catch { }
                }
                _pipes.Remove(remoteId);
            }
        }

        #endregion

        #region IO
        private async void SendHanler(object sender, System.Timers.ElapsedEventArgs e)
        {
            Tuple<string, byte[]>[] sendData;
            lock (_pipesLocker)
            {
                sendData = _sendBuffer.ToArray();
                _sendBuffer.Clear();
            }
            var sendTasks = sendData
                .GroupBy(a => a.Item1)
                .Select(a => SendToClient(a.Select(b => b.Item2).ToArray(), a.Key))
                .ToArray();
            await Task.WhenAll(sendTasks);
        }

        private async Task SendToClient(IEnumerable<byte[]> toSends, string key)
        {
            bool entered = false;
            TcpPartner trg = null;
            var token = _cancelToken.Token;
            try
            {
                trg = _pipes[key];
                var sendStream = trg.TcpStream;
                await trg.SendSemaphore.WaitAsync(token);
                entered = true;
                foreach (var t in toSends)
                {
                    if (token.IsCancellationRequested) return;
                    var bs = BitConverter.GetBytes(t.Length);
                    await sendStream.WriteAsync(bs, 0, bs.Length, token);
                    await sendStream.WriteAsync(t, 0, t.Length, token);
                }
                await sendStream.FlushAsync(token);
            }
            catch (Exception ex)
            {
                ErrorHandler(ex);
            }
            finally
            {
                if (entered && trg != null)
                    trg.SendSemaphore.Release();
            }
        }

        private async Task ReadClient(TcpPartner partner, string remoteId)
        {
            var token = _cancelToken.Token;
            var stream = partner.TcpStream;
            var wstream = new BinaryReader(stream);
            try
            {
                while (token.IsCancellationRequested && !partner.Disposed)
                {
                    var length = wstream.ReadInt32();
                    if (length >= 0)
                    {
                        var buf = new byte[length];
                        int cnt = 0;
                        do
                        {
                            cnt += await stream.ReadAsync(buf, cnt, length - cnt, token);
                        } while (cnt < length && !token.IsCancellationRequested);
                        await ProcessData(buf, remoteId, stream);
                    }
                    await Task.Delay(_configuration.FlushInterval, token);
                }
            }
            catch { }
        }

        private Task ProcessData(byte[] buf, string remoteId, Stream stream)
        {
            throw new NotImplementedException();
        }
        #endregion
        private void ErrorHandler(Exception exception)
        {
            throw new NotImplementedException();
            //{"Если базовый поток недоступен для поиска, запись в объект BufferedStream будет невозможна, "
            // + "пока буфер чтения не станет пустым. Убедитесь, что поток, базовый для объекта BufferedStream, "
            // + "доступен для поиска, или не выполняйте чередующиеся операции записи и чтения в этом объекте."}
        }
        private class NetworkConfig
        {
            internal int Port { get; set; }
            internal int FlushInterval { get; set; }
            internal string LocalId { get; set; }
        }
    }
}