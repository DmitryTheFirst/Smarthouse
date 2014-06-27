using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Smarthouse
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world!");



            /*var type = Type.GetType("Smarthouse.Program, Smarthouse, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
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

            */

            Console.ReadKey();
        }
    }

    class AccountManager:Module
    {
        private Dictionary<string, user> users;

        public AccountManager()
        {
            users = new Dictionary<string, user>();
        }

        class user
        {
            private string name;
            private string hashpass;
            private DateTime date;
            private string last_login_module;
        }

        public override bool Init()
        {
            throw new NotImplementedException();
        }

        public override bool Die()
        {
            throw new NotImplementedException();
        }

        public override bool ExecString()
        {
            throw new NotImplementedException();
        }
    }
}
