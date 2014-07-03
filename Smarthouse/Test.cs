using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse
{
    class Test : IModule
    {
        public Dictionary<string, string> Description { get; set; }
        public string StrongName { get; set; }
        public string CfgPath { get; set; }

        public bool Init()
        {
            return false;
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
