using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Xml;

namespace Smarthouse.Modules.Terminal
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Terminal : IRealModule, ITerminal, IRemote
    {
        /*Terminal can be not one. They all connected through the local network. Not implemented yet*/

        private ConsoleColor errorColor = ConsoleColor.Red;

        private string loginMessage = "Welcome, motherfucker! Who the fuck are you?";
        private byte tryesToLogin = 3;
        public ServiceHost WcfHost { get; set; }
        public string StubClassName { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }

        public bool Init()
        {
            bool success = true;
            //creating thread

            return success;
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

        public int ReadInt(string message, string errorMessage, ConsoleColor messageColor)
        {
            int result;
            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
            while (!int.TryParse(Console.ReadLine(), out result))
            {
                Console.ForegroundColor = errorColor;
                Console.WriteLine(errorMessage);
            }
            Console.ResetColor();
            return result;
        }

        public string ReadLine(string message, ConsoleColor messageColor)
        {
            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
            Console.ResetColor();
            return Console.ReadLine();
        }

        public void WriteLine(string message, ConsoleColor messageColor)
        {
            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

    }
}