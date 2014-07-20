using System;
using System.ServiceModel;

namespace Smarthouse.Modules.Terminal
{
    [ServiceContract]
    interface ITerminal
    {
        [OperationContract]
        int ReadInt(string message, string errorMessage, ConsoleColor messageColor);
        [OperationContract]
        string ReadLine(string message, ConsoleColor messageColor);
        [OperationContract]
        void WriteLine(string message, ConsoleColor messageColor);
    }
}
