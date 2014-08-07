using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Smarthouse.Modules
{
    class BoolCallable
    {
        protected Dictionary<string, Action<bool>> events { get; set; }

        public void CallEvent(string caller, bool state)
        {
            Action<bool> method;
            if (events.TryGetValue(caller, out method))
            {
                method.Invoke(state);
            }
            else
            {
                Console.WriteLine("Someone tried to call method that doesn't exist. Caller: {0}", caller);
            }
        }

        protected void ParseMethodsFromCfg(XmlNode Cfg)
        {
            events = new Dictionary<string, Action<bool>>();
            foreach (XmlElement eventCfg in Cfg.SelectSingleNode("events").ChildNodes.OfType<XmlElement>())
            {
                var method = this.GetType().
                    GetMethod(eventCfg.Attributes["method"].Value, BindingFlags.NonPublic | BindingFlags.Instance);
                events.Add(eventCfg.Attributes["caller"].Value,
                    (Action<bool>)method.CreateDelegate(typeof(Action<bool>), this)
                    );
            }
        }
    }
}
