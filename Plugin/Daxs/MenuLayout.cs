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

        private const byte KEY_UP = 0x26, KEY_DOWN = 0x28, KEY_TAB = 0x09, KEY_SHIFT = 0x10, KEY_ESCAPE = 0x1B, KEY_ENTER = 0x0D;

        public void HandleInput(RhinoDoc doc, RhinoView view, RhinoViewport vp, GamepadState state, GamepadState prevState, ref string displayMessage, ref DateTime lastPressedTime)
        {
            //Enter
            if (state.A && !prevState.A)
            {
                RhinoApp.WriteLine("state.A");
               // Eto.Forms.Form.ActiveForm.Close();
            }

            //Escape
            if (state.B && !prevState.B)
            {
                RhinoApp.WriteLine("state.B");
                SimulateKey(KEY_ESCAPE);
            }

            if (state.DPadRight && !prevState.DPadRight)
            {
                RhinoApp.WriteLine("DPadRight");
                SimulateKey(KEY_UP);
            }

            if (state.DPadLeft && !prevState.DPadLeft)
            {
                RhinoApp.WriteLine("DPadLeft");
                SimulateKey(KEY_DOWN);
            }

            if (state.DPadUp && !prevState.DPadUp)
            {
                RhinoApp.WriteLine("DPadUp");
                SimulateCombinedKey(KEY_SHIFT, KEY_TAB);
            }

            if (state.DPadDown && !prevState.DPadDown)
            {
                 RhinoApp.WriteLine("DPadDown");
                SimulateKey(KEY_TAB);
            }


            lastPressedTime = DateTime.Now;
        }

        // Simulate Arrow Down key press and release
        private void SimulateKey(byte bVk)
        {
            keybd_event(bVk, 0, 0x0000, UIntPtr.Zero); //KEYDOWN
            keybd_event(bVk, 0, 0x0002, UIntPtr.Zero); //KEYUP
        }

        private void SimulateCombinedKey(byte modifier, byte key)
        {
            keybd_event(modifier, 0, 0x0000, UIntPtr.Zero); // Modifier down (e.g., Shift)
            keybd_event(key, 0, 0x0000, UIntPtr.Zero);      // Key down (e.g., Tab)

            keybd_event(key, 0, 0x0002, UIntPtr.Zero);      // Key up
            keybd_event(modifier, 0, 0x0002, UIntPtr.Zero); // Modifier up
        }
    }
}