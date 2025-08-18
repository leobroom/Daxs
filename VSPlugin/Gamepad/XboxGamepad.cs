//#r "nuget: SharpDX.DirectInput, 4.2.0"
using SharpDX.XInput;

namespace Daxs
{
    public class XboxGamepad : IGamepad
    {
        private SharpDX.XInput.Controller controller;

        public XboxGamepad() => controller = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.One);

        public bool IsConnected => controller.IsConnected;

        public GamepadState GetState()
        {
            var pad = controller.GetState().Gamepad;
            var buttons = pad.Buttons;

            return new GamepadState
            (
                A: (buttons & GamepadButtonFlags.A) != 0,
                B: (buttons & GamepadButtonFlags.B) != 0,
                X: (buttons & GamepadButtonFlags.X) != 0,
                Y: (buttons & GamepadButtonFlags.Y) != 0,

                Start: (buttons & GamepadButtonFlags.Start) != 0,
                Back: (buttons & GamepadButtonFlags.Back) != 0,

                DPadUp: (buttons & GamepadButtonFlags.DPadUp) != 0,
                DPadDown: (buttons & GamepadButtonFlags.DPadDown) != 0,
                DPadLeft: (buttons & GamepadButtonFlags.DPadLeft) != 0,
                DPadRight: (buttons & GamepadButtonFlags.DPadRight) != 0,

                L1: (buttons & GamepadButtonFlags.LeftShoulder) != 0,
                L2: pad.LeftTrigger / 255f,
                L3: (buttons & GamepadButtonFlags.LeftThumb) != 0,

                R1: (buttons & GamepadButtonFlags.RightShoulder) != 0,
                R2: pad.RightTrigger / 255f,
                R3: (buttons & GamepadButtonFlags.RightThumb) != 0,

                LeftThumbX: pad.LeftThumbX / 32767.0,
                LeftThumbY: pad.LeftThumbY / 32767.0,
                RightThumbX: pad.RightThumbX / 32767.0,
                RightThumbY: pad.RightThumbY / 32767.0
            );
        }
    }
}