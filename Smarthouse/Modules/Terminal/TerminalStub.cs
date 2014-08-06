using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Smarthouse.Modules.Hardware.Led;

namespace Smarthouse.Modules.Terminal
{
    class TerminalStub : IStubModule, ITerminal
    {
        public Dictionary<string, string> Description { get; set; }
        public IPEndPoint RealAddress { get; set; }
        #region Channel
        private ChannelFactory<ITerminal> MyChannelFactory { get; set; }
        private ITerminal _transparentProxy;
        #endregion
        public event EventHandler Dead;
        public bool Init()
        {
            MyChannelFactory = new ChannelFactory<ITerminal>(new BasicHttpBinding(),
                                       "http://" + RealAddress.Address + ":" + RealAddress.Port + "/" + Description["name"]
                   );
            Console.WriteLine(Description["name"] + " stub just initiated");
            return true;
        }

        public bool Start()
        {
            _transparentProxy = MyChannelFactory.CreateChannel();
            Console.WriteLine(Description["name"] + " stub just started");
            return true;
        }

        public void Die()
        {
            if (Dead != null)
                Dead.Invoke(null, null);
        }

        public int ReadInt(string message, string errorMessage, ConsoleColor messageColor)
        {
            throw new NotImplementedException();
        }

        public string ReadLine(string message, ConsoleColor messageColor)
        {
            throw new NotImplementedException();
        }

        public void WriteLine(string message, ConsoleColor messageColor)
        {
            try
            {
                _transparentProxy.WriteLine(message, messageColor);
            }
            catch (Exception ex)
            {
                Die();
                throw new Exception("Connection problems");
            }
        }

    }
}
