using System.ServiceModel;

namespace Smarthouse.Modules.Hardware.Button
{
    [ServiceContract]
    interface IButton
    {
        [OperationContract]
        bool GetState();
    }
}
