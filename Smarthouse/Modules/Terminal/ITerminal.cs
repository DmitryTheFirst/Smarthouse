using System;

namespace Smarthouse.Modules.Terminal
{
    interface ITerminal
    {
        int ReadInt(string message, string errorMessage, ConsoleColor messageColor);
        string ReadLine(string message, ConsoleColor messageColor);
        void WriteLine(string message, ConsoleColor messageColor);
    }
}
