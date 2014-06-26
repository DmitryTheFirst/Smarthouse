using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Smarthouse
{
    class Program : ISmarthousable
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world!");
            
            List<ISmarthousable> lst = new List<ISmarthousable>();
            var type = Type.GetType("Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            if (type == null)
                throw new TypeLoadException("Can't load specified parser type");
            var ifaceType = typeof(ISmarthousable);
            if (type.GetInterface(ifaceType.Name, false) != null)
            {
                lst.Add((ISmarthousable)Activator.CreateInstance(type));
                //------------------------
            }
            foreach ( var VARIABLE in lst )
            {
                VARIABLE.Init();
            }


            //cfg get cfg


            Console.ReadKey();
        }

        public bool Init()
        {
            Console.WriteLine( "1" );
            return true;
        }
    }


    interface ISmarthousable
    {
        bool Init();
    }
}
