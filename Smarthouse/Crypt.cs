using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Smarthouse
{
    public class Crypt : IModule, ICrypt
    {
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public Dictionary<string, Func<byte[]>> MethodResolver { get; set; }
        public bool Stub { get; set; }
        public EndPoint RealIp { get; set; }
        public string StubCryptModuleName { get; set; }
        public INetwork UsingNetwork { get; set; }
        public string PartnerNetworkId { get; set; }

        public bool Init()
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public void ExecSerializedCommand( string user, byte[] data )
        {
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
    }
}