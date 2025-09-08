using SharpDX.XInput;

namespace Daxs
{
    public class XboxGamepad : Gamepad
    {
        private readonly SharpDX.XInput.Controller controller;

        public XboxGamepad() => controller = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.One);

        public override bool IsConnected => controller.IsConnected;

        public override GamepadState GetState()
        {
            var pad = controller.GetState().Gamepad;
            var buttons = pad.Buttons;

            previous = current;
            current = new GamepadState
            (
                A: GetInputState((buttons & GamepadButtonFlags.A) != 0, previous.A),
                B: GetInputState((buttons & GamepadButtonFlags.B) != 0, previous.B),
                X: GetInputState((buttons & GamepadButtonFlags.X) != 0, previous.X),
                Y: GetInputState((buttons & GamepadButtonFlags.Y) != 0, previous.Y),

                Start: GetInputState((buttons & GamepadButtonFlags.Start) != 0, previous.Start),
                Back: GetInputState((buttons & GamepadButtonFlags.Back) != 0, previous.Back),

                DPadUp: GetInputState((buttons & GamepadButtonFlags.DPadUp) != 0, previous.DPadUp),
                DPadDown: GetInputState((buttons & GamepadButtonFlags.DPadDown) != 0, previous.DPadDown),
                DPadLeft: GetInputState((buttons & GamepadButtonFlags.DPadLeft) != 0, previous.DPadLeft),
                DPadRight: GetInputState((buttons & GamepadButtonFlags.DPadRight) != 0, previous.DPadRight),

                L1: GetInputState((buttons & GamepadButtonFlags.LeftShoulder) != 0, previous.L1),
                L2: pad.LeftTrigger / 255f,
                L3: GetInputState((buttons & GamepadButtonFlags.LeftThumb) != 0, previous.L3),

                R1: GetInputState((buttons & GamepadButtonFlags.RightShoulder) != 0, previous.R1),
                R2: pad.RightTrigger / 255f,
                R3: GetInputState((buttons & GamepadButtonFlags.RightThumb) != 0, previous.R3),

                LeftThumbX: pad.LeftThumbX / 32767.0f,
                LeftThumbY: pad.LeftThumbY / 32767.0f,
                RightThumbX: pad.RightThumbX / 32767.0f,
                RightThumbY: pad.RightThumbY / 32767.0f
            );

            return current;
        }
    }
}