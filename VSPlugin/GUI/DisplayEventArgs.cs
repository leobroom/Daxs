using System;

namespace Daxs.GUI
{
    public class DisplayEventArgs : EventArgs
    {
        public string Message { get; }
        public DisplayEventArgs(string message) => Message = message;
    }
}