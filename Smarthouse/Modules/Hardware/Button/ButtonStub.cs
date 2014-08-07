using System;
using System.Collections.Generic;
using System.Net;

namespace Smarthouse.Modules.Hardware.Button
{

    class ButtonStub : IStubModule, IButton
    {
        public Dictionary<string, string> Description { get; set; }
        public bool Init()
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public event EventHandler Dead;
        public IPEndPoint RealAddress { get; set; }
        public bool GetState()
        {
            throw new NotImplementedException();
        }
    }
}
