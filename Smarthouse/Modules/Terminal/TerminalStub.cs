using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Smarthouse.Modules.Terminal
{
    class TerminalStub : IStubModule, ITerminal
    {
        public Dictionary<string, string> Description { get; set; }
        public IPEndPoint RealAddress { get; set; }
        private ITerminal _transparentProxy;
        public bool Init()
        {
            ChannelFactory<ITerminal> myChannelFactory = new ChannelFactory<ITerminal>(new BasicHttpBinding(),
                                    "http://" + RealAddress.Address + ":" + RealAddress.Port + "/" + Description["name"]
                );
            _transparentProxy = myChannelFactory.CreateChannel();
            return true;
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public bool Die()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

    }
}
