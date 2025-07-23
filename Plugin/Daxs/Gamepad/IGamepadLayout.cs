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
        void HandleInput(RhinoDoc doc, 
                        RhinoView view, 
                        RhinoViewport vp, 
                        GamepadState state, 
                        GamepadState prevState, 
                        ref string displayMessage, 
                        ref DateTime lastPressedTime);
    }
}