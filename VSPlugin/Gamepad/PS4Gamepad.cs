using System;
using SharpDX.DirectInput;
using System.Linq;
using Rhino;

namespace Daxs
{
    public class PS4Gamepad : Gamepad
    {
        public override bool IsConnected => joystick != null;

        private readonly DirectInput directInput;
        private readonly Joystick joystick;

        public PS4Gamepad()
        {
            directInput = new DirectInput();

            var devices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            var ps4Device = devices.FirstOrDefault(); // Or filter for specific name/product GUID if needed

            if (ps4Device != null)
            {
                joystick = new Joystick(directInput, ps4Device.InstanceGuid);
                joystick.Properties.BufferSize = 128;
                try
                {joystick.Acquire();}
                catch (SharpDX.SharpDXException ex)
                {
                    Console.WriteLine("Acquire failed: " + ex.Message);
                    joystick = null;
                }
            }
            else
                RhinoApp.WriteLine("ps4Device == null");
        }
        public override GamepadState GetState()
        {
            if (joystick == null)
            {
                RhinoApp.WriteLine("joystick == null");
                return new GamepadState();
            }

            var state = joystick.GetCurrentState();
            if (state == null || joystick.IsDisposed)
            {
                string st = (state == null) ? "null" : state.ToString();

                RhinoApp.WriteLine($"joystick IsDisposed= ${joystick.IsDisposed} || state == ${st}  ");
                return new GamepadState();
            }

            var buttons = state.Buttons;
            var pov = state.PointOfViewControllers.FirstOrDefault();

            previous = current;
            current = new GamepadState
            (
                A: GetInputState(buttons[1], previous.A),
                B: GetInputState(buttons[2], previous.B),
                X: GetInputState(buttons[0], previous.X),
                Y: GetInputState(buttons[3], previous.Y),

                Start: GetInputState(buttons[9], previous.Start),
                Back: GetInputState(buttons[8], previous.Back),

                L1: GetInputState(buttons[4], previous.L1),
                L2: (buttons[6]) ? state.RotationX / 65535f : 0,
                L3: GetInputState(buttons[10], previous.L3),

                R1: GetInputState(buttons[5], previous.R1),
                R2: (buttons[7]) ? state.RotationY / 65535f : 0,
                R3: GetInputState(buttons[11], previous.R3),

                DPadUp: GetInputState(pov == 0 || pov == 4500 || pov == 31500, previous.DPadUp),
                DPadDown: GetInputState(pov == 13500 || pov == 18000 || pov == 22500, previous.DPadDown),
                DPadLeft: GetInputState(pov == 18000 || pov == 27000 || pov == 31500, previous.DPadLeft),
                DPadRight: GetInputState(pov == 4500 || pov == 9000 || pov == 13500, previous.DPadRight),

                LeftThumbX: (state.X - 32768f) / 32768f,
                LeftThumbY: -(state.Y - 32768f) / 32768f,
                RightThumbX: (state.Z - 32768f) / 32768f,
                RightThumbY: -(state.RotationZ - 32768f) / 32768f
            );

            return current;
        }
    }
}