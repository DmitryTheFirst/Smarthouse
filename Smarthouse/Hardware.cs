using System;
using System.Collections.Generic;
using System.Xml;
using Raspberry.IO;
using Raspberry.IO.GeneralPurpose;

namespace Smarthouse
{
    class Hardware : IModule
    {
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public bool Init()
        {
            var led1 = ConnectorPin.P1Pin11.Output();
            var led2 = ConnectorPin.P1Pin13.Output();
            var connection1 = new GpioConnection(led1);
            var connection2 = new GpioConnection(led2);
            string s1;
            string s2;
           do
            {
                Console.WriteLine("input s1 & s2");
                s1= Console.ReadLine();
                s2 = Console.ReadLine();
                int i1,i2;

                if (! int.TryParse( s1, out i1 ) || !int.TryParse( s2, out i2 ) )
                {
                    Console.WriteLine("Wrong params");
                    continue;
                }

                connection1.Toggle(led1);
                System.Threading.Thread.Sleep(i1);
               connection1.Toggle(led1);

                connection2.Blink(led2, i2);
                
            } while (s1 != "end"|| s2 != "end");
            connection1.Close();
            connection2.Close();
            return true;
        }

        public bool Start()
        {
            return true;
        }

        public bool Die()
        {
            throw new System.NotImplementedException();
        }
    }
}