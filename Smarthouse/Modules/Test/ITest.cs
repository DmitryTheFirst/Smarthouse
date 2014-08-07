using System;
using System.ServiceModel;

namespace Smarthouse.Modules.Test
{
    [ServiceContract]
    interface ITest
    {

        [OperationContract]
        int GetRandomNum(int min, int max);

    }
}
