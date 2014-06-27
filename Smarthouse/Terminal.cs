using System;
using System.Drawing;
using System.Threading;

namespace Smarthouse
{

    class Terminal : Module
    {
        /*Terminal can be not one. They all connected through the local network. Not implemented yet*/

        private ConsoleColor errorColor = ConsoleColor.Red;
        
        private string loginMessage = "Welcome, motherfucker! Who the fuck are you?";
        private byte tryesToLogin = 3;

        public override bool Init()
        {
            bool success = true;
            //creating thread

            return success;
        }

        private void Work()
        {
            string login = this.ReadLine( loginMessage, ConsoleColor.Green );
            
            do
            {
               
            } while ( true );//i can't use normal flags because there will be thread blocker(console.readline). So i'll just kill thread  
        }

        public override bool Die()
        {
            throw new NotImplementedException();
        }

        public override bool ExecString()
        {
            throw new NotImplementedException();
        }

        public bool Login()
        {
            
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