using System;
using System.ServiceModel;

namespace Smarthouse.Modules
{
    interface IRemote
    {
        ServiceHost WcfHost { get; set; }
        Type StubClass { get; set; }
    }
}
