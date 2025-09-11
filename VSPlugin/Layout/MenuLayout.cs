using Rhino;
using System;
using System.Runtime.InteropServices;

namespace Daxs
{
    public class MenuLayout : IGamepadLayout
    {
        public Layout Name => Layout.Menu;
        private readonly HUD hud = HUD.Instance;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte KEY_UP = 0x26, KEY_DOWN = 0x28, KEY_TAB = 0x09, KEY_SHIFT = 0x10, KEY_ESCAPE = 0x1B, KEY_ENTER = 0x0D;
        public void HandleInput(GamepadState state, double delta)
        {
            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                //Enter
                if (state.A == InputX.IsDown)
                {
                    hud.SetText("Enter", 2000);
                    SimulateKey(KEY_ENTER);
                }

                //Escape
                if (state.B == InputX.IsDown || state.Start == InputX.IsDown)
                {
                    hud.SetText("Escape", 2000);
                    SimulateKey(KEY_ESCAPE);
                }

                if (state.DPadRight == InputX.IsDown)
                    SimulateKey(KEY_UP);

                if (state.DPadLeft == InputX.IsDown)
                    SimulateKey(KEY_DOWN);

                if (state.DPadUp == InputX.IsDown)
                    SimulateCombinedKey(KEY_SHIFT, KEY_TAB);

                if (state.DPadDown == InputX.IsDown)
                    SimulateKey(KEY_TAB);
            }));
        }

        // Simulate Arrow Down key press and release
        private static void SimulateKey(byte bVk)
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