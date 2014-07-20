using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Smarthouse.Modules.Test
{
    [ServiceContract]
    interface ITest:IModule
    {

        [OperationContract]
        int GetRandomNum(int min, int max);
    }
}
