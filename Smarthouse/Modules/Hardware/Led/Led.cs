using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Xml;
using Smarthouse.Modules.Hardware.Button;
using Smarthouse.Modules.Terminal;

namespace Smarthouse.Modules.Hardware.Led
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class Led : BoolCallable, IRealModule, ILed, IRemote
    {
        private byte _wiringPiPin;

        public void SetState(bool state)
        {
            WiringPi.digitalWrite(_wiringPiPin, state ? (int)WiringPi.PinSignal.HIGH : (int)WiringPi.PinSignal.LOW);
            Console.WriteLine("Set state " + state);
        }

        public Dictionary<string, string> Description { get; set; }

        public bool Init()
        {
            #region Parse from cfg
            _wiringPiPin = byte.Parse(Cfg.SelectSingleNode("hardware").Attributes["pin"].Value);
            ParseMethodsFromCfg(Cfg);
            #endregion
            WiringPi.Setup();
            WiringPi.pinMode(_wiringPiPin, (int)WiringPi.PinMode.OUTPUT);
            return true;
        }

        public bool Start()
        {
            //load state from cfg?
            WiringPi.digitalWrite(_wiringPiPin, (int)WiringPi.PinSignal.LOW);
            return true;
        }

        public void Die()
        {
            if (Dead != null)
                Dead.Invoke(null, null);
        }

        public event EventHandler Dead;
        public XmlNode Cfg { get; set; }
        public ModuleManager ModuleManager { get; set; }

        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }

        private void btn1Clicked(bool state)
        {
            Console.WriteLine("Button 1 said " + state);
            if (state)
            {
                WiringPi.digitalWrite(_wiringPiPin, (int)WiringPi.PinSignal.HIGH);
            }

        }
        private void btn2Clicked(bool state)
        {
            Console.WriteLine("Button 2 said " + state);
            if (state)
            {
                WiringPi.digitalWrite(_wiringPiPin, (int)WiringPi.PinSignal.LOW);
            }

        }
        private void btn3Clicked(bool state)
        {
            Console.WriteLine("Button 3 said " + state);
        }
    }
}