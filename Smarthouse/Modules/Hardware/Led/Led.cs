using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Xml;
using Smarthouse.Modules.Terminal;

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
            if (Dead != null)
                Dead.Invoke(null, null);
        }

        public event EventHandler Dead;
        public XmlNode Cfg { get; set; }
        public ModuleManager ModuleManager { get; set; }
        public void SetState(bool state)
        {
            WiringPi.digitalWrite(wiringPiPin, state ?
                (int)WiringPi.PinSignal.HIGH :
                (int)WiringPi.PinSignal.LOW);
            var terminal = (ITerminal)ModuleManager.FindModule("name", "TerminalMain");
            Console.WriteLine(terminal);
            terminal.WriteLine("Led " + state, state ? ConsoleColor.Green : ConsoleColor.Red);
        }

        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }
    }
}
