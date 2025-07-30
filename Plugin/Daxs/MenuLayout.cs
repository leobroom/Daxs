using Rhino;
using Rhino.Display;
using System;
using System.Runtime.InteropServices;

namespace Daxs
{
    public class MenuLayout : IGamepadLayout
    {
        public string Name => "Menu";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


        private const byte VK_DOWN = 0x28;
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        public void HandleInput(RhinoDoc doc, RhinoView view, RhinoViewport vp, GamepadState state, GamepadState prevState, ref string displayMessage, ref DateTime lastPressedTime)
        {

       
            if (state.A && !prevState.A)
            {
                RhinoApp.WriteLine("state.A");
               // RhinoApp.RunScript("_Enter", false);
                //displayMessage = "UI A pressed";
                //lastPressedTime = DateTime.Now;
            }


            // D-Pad Down pressed

            if (state.DPadDown)
            {
                RhinoApp.WriteLine("D-Pad Down................");
            }

            if (state.DPadDown && !prevState.DPadDown)
            {
                RhinoApp.WriteLine("D-Pad Down &UP");

                // Simulate Arrow Down key press and release
                keybd_event(VK_DOWN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                displayMessage = "D-Pad ↓ pressed";
                lastPressedTime = DateTime.Now;
            }


        }
    }
}