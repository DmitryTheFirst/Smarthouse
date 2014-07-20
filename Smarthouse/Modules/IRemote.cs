using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse.Modules
{
    interface IRemote
    {
        ServiceHost WcfHost { get; set; }
    }
}
