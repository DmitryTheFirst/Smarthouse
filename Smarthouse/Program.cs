using System;

namespace Smarthouse
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world!");
            Smarthouse sh = new Smarthouse();
            sh.EasyStart(args[0], true);
        }
    }


}
