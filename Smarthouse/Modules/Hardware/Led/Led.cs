﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Xml;

namespace Smarthouse.Modules.Hardware.Led
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Led : IRealModule, ILed, IRemote
    {
        public Dictionary<string, string> Description { get; set; }

        private byte wiringPiPin;

        public bool Init()
        {
            #region Parse from cfg
            wiringPiPin = byte.Parse(Cfg.SelectSingleNode("hardware").Attributes["pin"].Value);
            #endregion
            WiringPi.Setup();
            WiringPi.pinMode(wiringPiPin, (int)WiringPi.PinMode.OUTPUT);
            return true;
        }

        public bool Start()
        {
            //load state from cfg?
            return true;
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public event EventHandler Dead;
        public XmlNode Cfg { get; set; }
        public ModuleManager ModuleManager { get; set; }
        public void SetState(bool state)
        {
            WiringPi.digitalWrite(wiringPiPin, state ?
                (int)WiringPi.PinSignal.HIGH :
                (int)WiringPi.PinSignal.LOW);
        }

        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }
    }
}
