using System;
using System.Collections.Generic;
using System.Xml;

namespace Smarthouse
{
    class Crypt : IModule, ICrypt
    {
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public bool Init()
        {
            throw new NotImplementedException();
        }

        public bool Start()
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