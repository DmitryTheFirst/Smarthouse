using System;
using System.Collections.Generic;

namespace Smarthouse
{
    class Logger : Module
    {
        public Dictionary<string, HashSet<string>> description { get; set; }
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