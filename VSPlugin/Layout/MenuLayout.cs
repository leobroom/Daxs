using Rhino;
using System;
using System.Runtime.InteropServices;
using static SDL3.SDL;

namespace Daxs.Layout
{
    internal class MenuLayout : BaseLayout
    {
        public override LayoutType Name => LayoutType.Menu;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte KEY_UP = 0x26, KEY_DOWN = 0x28, KEY_TAB = 0x09, KEY_SHIFT = 0x10, KEY_ESCAPE = 0x1B, KEY_ENTER = 0x0D, KEY_LEFT = 0x25, KEY_RIGHT = 0x27;
        public override void HandleInput(Gamepad state)
        {
            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                //Enter
                if (state.GetButtonState(GamepadButton.South) == InputX.IsDown)
                {
                    hud.SetText("🎮", "Enter");
                    SimulateKey(KEY_ENTER);
                }

                //Escape
                if (state.GetButtonState(GamepadButton.East) == InputX.IsDown || state.GetButtonState(GamepadButton.Start) == InputX.IsDown)
                {
                    hud.SetText("🎮", "Escape");
                    SimulateKey(KEY_ESCAPE);
                }

                if (state.GetButtonState(GamepadButton.DPadRight) == InputX.IsDown)
                    SimulateKey(KEY_RIGHT);

                if (state.GetButtonState(GamepadButton.DPadLeft) == InputX.IsDown)
                    SimulateKey(KEY_LEFT);

                if (state.GetButtonState(GamepadButton.DPadUp) == InputX.IsDown)
                    SimulateCombinedKey(KEY_SHIFT, KEY_TAB);

                if (state.GetButtonState(GamepadButton.DPadDown) == InputX.IsDown)
                    SimulateKey(KEY_TAB);

                //TAB LEFT RIGHT
                if (state.GetButtonState(GamepadButton.LeftShoulder) == InputX.IsDown)
                    SimulateKey(KEY_DOWN);

                if (state.GetButtonState(GamepadButton.RightShoulder) == InputX.IsDown)
                    SimulateKey(KEY_UP);
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