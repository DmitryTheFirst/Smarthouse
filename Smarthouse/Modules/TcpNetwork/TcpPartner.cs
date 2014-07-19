using System;
using System.IO;
using System.Threading;

namespace Smarthouse.Modules.TcpNetwork
{
    internal class TcpPartner : IDisposable
    {
        public TcpPartner(Stream partnerStream, string username, string CryptName)
        {
            SendSemaphore = new SemaphoreSlim(1);
            ReadSemaphore = new SemaphoreSlim(1);
            TcpStream = partnerStream;
            Username = username;
            if (!String.IsNullOrWhiteSpace(CryptName))
                Crypt = (Crypt.Crypt)Smarthouse.ModuleManager.FindModule("name", CryptName);

        }

        public Stream TcpStream { get; set; }
        public string Username { get; set; }
        public Crypt.Crypt Crypt { get; set; }

        public readonly SemaphoreSlim SendSemaphore;
        public readonly SemaphoreSlim ReadSemaphore;
        public bool Disposed { get; protected set; }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            SendSemaphore.Dispose();
            ReadSemaphore.Dispose();
        }

        ~TcpPartner()
        {
            Dispose();
        }
    }
}