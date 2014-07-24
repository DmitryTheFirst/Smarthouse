using System.Net;

namespace Smarthouse.Modules
{
    interface IStubModule : IModule
    {
        IPEndPoint RealAddress { get; set; }
    }
}
