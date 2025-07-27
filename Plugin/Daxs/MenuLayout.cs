using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    public class MenuLayout : IGamepadLayout
    {
        public string Name => "Menu";

        public void HandleInput(RhinoDoc doc, RhinoView view, RhinoViewport vp, GamepadState state, GamepadState prevState, ref string displayMessage, ref DateTime lastPressedTime)
        {
            if (state.A && !prevState.A)
            {
                RhinoApp.RunScript("_Enter", false);
                displayMessage = "UI A pressed";
                lastPressedTime = DateTime.Now;
            }
        }
    }
}