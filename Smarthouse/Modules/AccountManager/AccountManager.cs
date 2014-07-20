using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Xml;

namespace Smarthouse.Modules.AccountManager
{
    internal class AccountManager : IModule
    {
        public Dictionary<string, string> Description { get; set; }
        public XmlNode Cfg { get; set; }
        private Dictionary<string, User> users;

        private byte maxLoginFailes = 3;
        private TimeSpan delay = new TimeSpan(0, 3, 0); //3 minutes delay after 3 wrong passes 
        private SHA1 sha1 = new SHA1CryptoServiceProvider();

        public AccountManager()
        {
            users = new Dictionary<string, User>();
        }

        private bool CheckPassword(string username, string password, string moduleFriendlyName)
        {
            bool success;
            if (!users.ContainsKey(username)) //no such user
                return false;
            User user = users[username];
            if ((user.failLogins.Count < maxLoginFailes)
                 || (DateTime.Now - user.failLogins[user.failLogins.Count - maxLoginFailes].date > delay))
            {
                success = Hash(password).Equals(user.hashpass);
            }
            else
            {
                success = false;
            }

            if (success)
            {
                user.lastSuccessDate = DateTime.Now;
                user.last_login_module = moduleFriendlyName;
            }
            else
            {
                user.failLogins.Add(new User.Login(moduleFriendlyName, DateTime.Now));
            }
            return success;
        }

        private byte[] Hash(string password)
        {
            return sha1.ComputeHash(GetBytes(password));
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }




        public bool Init()
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public bool Die()
        {
            throw new NotImplementedException();
        }

        private class User
        {
            public string name;
            public byte[] hashpass;
            public DateTime? lastSuccessDate;
            public string last_login_module;
            public List<Login> failLogins;

            public User(string name, byte[] hashpass)
            {
                this.name = name;
                this.hashpass = hashpass;
            }

            public class Login
            {
                public DateTime date;
                public string module;

                public Login(string module, DateTime date)
                {
                    this.module = module;
                    this.date = date;
                }
            }

        }
    }


}