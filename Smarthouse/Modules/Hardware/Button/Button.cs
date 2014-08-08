using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Xml;

namespace Smarthouse.Modules.Hardware.Button
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Button : IRealModule, IButton, IRemote
    {
        public Dictionary<string, string> Description { get; set; }
        private byte wiringPiPin;
        Thread btnListener;
        private int _betweenIterationsMilliseconds;
        private bool _state;
        private string _myName;
        public bool Init()
        {
            #region Parse from cfg
            wiringPiPin = byte.Parse(Cfg.SelectSingleNode("hardware").Attributes["pin"].Value);
            _betweenIterationsMilliseconds = int.Parse(Cfg.SelectSingleNode("hardware").Attributes["betweenIterationsMilliseconds"].Value);
            #endregion
            _myName = Description["name"];
            WiringPi.Setup();
            WiringPi.pinMode(wiringPiPin, (int)WiringPi.PinMode.INPUT);
            WiringPi.pullUpDnControl(wiringPiPin, (int)WiringPi.PullResistor.PUD_DOWN);
            btnListener = new Thread(BtnListen);
            return true;
        }

        public bool Start()
        {
            btnListener.Start();
            return true;
        }
        void BtnListen()
        {
            do
            {
                bool signal = WiringPi.digitalRead(wiringPiPin) == 1;
                if (signal != _state)
                {
                    _state = signal;
                    StateChanged(signal);
                }
                Thread.Sleep(_betweenIterationsMilliseconds);
            } while (true);
        }

        void StateChanged(bool state)
        {
            var subscribers = ModuleManager.GetAllModulesByType<BoolCallable>();
            foreach (var subscriber in subscribers)
            {
                subscriber.CallEvent(_myName, state);
            }
        }

        public void Die()
        {
            btnListener.Abort();
            if (Dead != null)
                Dead.Invoke(null, null);
        }

        public event EventHandler Dead;
        public XmlNode Cfg { get; set; }
        public ModuleManager ModuleManager { get; set; }
        public bool GetState()
        {
            return _state;
        }

        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }
    }
}
