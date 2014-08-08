using System.ServiceModel;

namespace Smarthouse.Modules.Hardware.Button
{
    [ServiceContract]
    interface IDigitalInput
    {
        [OperationContract]
        bool GetState();
    }
}
