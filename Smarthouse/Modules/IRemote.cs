using System.ServiceModel;

namespace Smarthouse.Modules
{
    interface IRemote
    {
        ServiceHost WcfHost { get; set; }
        string StubClassName { get; set; }
    }
}
