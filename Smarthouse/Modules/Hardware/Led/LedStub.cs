using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;

namespace Smarthouse.Modules.Hardware.Led
{
    class LedStub : IStubModule, ILed
    {
        public Dictionary<string, string> Description { get; set; }

        #region Channel
        private ChannelFactory<ILed> MyChannelFactory { get; set; }
        private ILed _transparentProxy;
        #endregion
        public bool Init()
        {
            MyChannelFactory = new ChannelFactory<ILed>(new BasicHttpBinding(),
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
            throw new NotImplementedException();
        }

        public event EventHandler Dead;
        public IPEndPoint RealAddress { get; set; }


        public void SetState(bool state)
        {
            _transparentProxy.SetState(state);
        }
    }
}
