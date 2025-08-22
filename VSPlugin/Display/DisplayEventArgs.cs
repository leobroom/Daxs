using System;

namespace Daxs
{
    public class DisplayEventArgs : EventArgs
    {
        public string Message { get; }
        public DisplayEventArgs(string message) => Message = message;
    }
}