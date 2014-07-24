using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse.Modules
{
    interface IStubModule : IModule
    {
        IPEndPoint RealAddress { get; set; }
    }
}
