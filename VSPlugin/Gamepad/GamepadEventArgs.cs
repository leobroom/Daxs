using System;

namespace Daxs
{
    public sealed class GamepadEventArgs : EventArgs
    {
        public Gamepad Gamepad { get; }
        public GamepadEventArgs(Gamepad gamepad) => Gamepad = gamepad;
    }
}
