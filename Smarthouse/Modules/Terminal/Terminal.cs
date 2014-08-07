﻿using System;
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

        public event EventHandler Dead;
        public ModuleManager ModuleManager { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }

        public bool Init()
        {
            var success = true;
            //creating thread

            return success;
        }
        public bool Start()
        {
            //Start thread listener for input
            //throw new NotImplementedException();
            return true;
        }

        public void Die()
        {
            if (Dead != null)
                Dead.Invoke(null, null);
        }

        public ServiceHost WcfHost { get; set; }
        public Type StubClass { get; set; }

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