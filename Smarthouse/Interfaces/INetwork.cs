using System.IO;
using System.Net;

namespace Smarthouse
{
    public interface INetwork : IModule
    {
        bool ConnectTo(EndPoint partnerUri, Crypt crypt, out string partnerId);
        bool SendTo(string partnerId, byte[] data);
    }
}