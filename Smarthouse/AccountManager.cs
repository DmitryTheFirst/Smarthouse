using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Smarthouse
{
    class AccountManager : Module
    {
        private Dictionary<string, User> users;
        private byte maxLoginFailes = 3;
        TimeSpan delay = new TimeSpan(0, 3, 0);//3 minutes delay after 3 wrong passes 
        SHA1 sha1 = new SHA1CryptoServiceProvider();
        public AccountManager()
        {
            this.users = new Dictionary<string, User>();
        }

        private bool CheckPassword(string username, string password, string moduleFriendlyName)
        {
            bool success;
            if (!this.users.ContainsKey(username)) //no such user
                return false;
            User user = this.users[username];
            if ((user.failLogins.Count < this.maxLoginFailes)
                || (DateTime.Now - user.failLogins[user.failLogins.Count - this.maxLoginFailes].date > this.delay))
            {
                success = this.Hash(password).Equals(user.hashpass);
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
            return this.sha1.ComputeHash(GetBytes(password));
        }
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        class User
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

        public Dictionary<string, string> description { get; set; }
        public string StrongName { get; set; }
        public bool Init()
        {
            throw new NotImplementedException();
        }

        public void Start()
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
    }
}