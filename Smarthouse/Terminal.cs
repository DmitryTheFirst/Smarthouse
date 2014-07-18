using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Smarthouse
{

    class Terminal : IModule
    {
        /*Terminal can be not one. They all connected through the local network. Not implemented yet*/

        private ConsoleColor errorColor = ConsoleColor.Red;
        
        private string loginMessage = "Welcome, motherfucker! Who the fuck are you?";
        private byte tryesToLogin = 3;

        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        public Dictionary<string, Func<byte[]>> MethodResolver { get; set; }
        public bool Stub { get; set; }

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

        public void ExecSerializedCommand( string user, byte[] data )
        {
            throw new NotImplementedException();
        }

        public bool Die()
        {
            throw new NotImplementedException();
        }

        public bool ExecString()
        {
            throw new NotImplementedException();
        }

        private void Work()
        {
            string login = ReadLine( loginMessage, ConsoleColor.Green );
            
            do
            {
               
            } while ( true );//i can't use normal flags because there will be thread blocker(console.readline). So i'll just kill thread  
        }

        

        public bool Login()
        {
            throw new NotImplementedException();
        }

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

        public void WriteLine( string message, ConsoleColor messageColor )
        {
            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }


    }
}