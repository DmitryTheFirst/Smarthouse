using System;

namespace Smarthouse
{
    interface INetwork
    {
        bool ConnectTo(Uri partnerUri);
        bool SendTo(Uri partnerUri);
    }
}