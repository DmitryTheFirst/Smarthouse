﻿using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;

namespace Smarthouse.Modules.Test
{
    class TestStub : IStubModule, ITest
    {
        public Dictionary<string, string> Description { get; set; }
        public IPEndPoint RealAddress { get; set; }
        private ChannelFactory<ITest> MyChannelFactory { get; set; }
        private ITest _transparentProxy;
        public bool Init()
        {
            MyChannelFactory = new ChannelFactory<ITest>(new BasicHttpBinding(),
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

        public int GetRandomNum(int min, int max)
        {
            Console.WriteLine(Description["name"] + " stub GetRandomNum called");
            return _transparentProxy.GetRandomNum(min, max);
        }

    }
}
