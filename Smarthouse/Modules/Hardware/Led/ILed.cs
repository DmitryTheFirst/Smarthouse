using System.ServiceModel;

namespace Smarthouse.Modules.Hardware.Led
{
    [ServiceContract]
    interface ILed
    {
        [OperationContract]
        void SetState(bool state);
    }
}
