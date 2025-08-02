// #! csharp
using System;
using Rhino;
using Rhino.Display;

namespace Daxs
{
    /// Abstract Interface to allow different layouts
    public interface IGamepadLayout
    {
        string Name { get; }
        void HandleInput( GamepadState state, GamepadState prevState);
    }
}