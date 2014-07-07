using System;
using System.Net;

namespace Smarthouse
{
    interface INetwork
    {
        bool ConnectTo(EndPoint partnerUri);
        bool SendTo(string partnerUri, byte[] data);
    }
}