using System.IO;
using System.Net;

namespace Smarthouse {
    public interface INetwork : IModule {
        bool AuthServer( Stream newPartner, out string cryptModuleName, out string remoteId );
        void AuthClient( Stream newPartner, string localId, string cryptModule );
        bool ConnectTo( EndPoint partnerUri, Crypt crypt, out string partnerId );
        bool SendTo( string partnerId, byte[] data );
    }
}