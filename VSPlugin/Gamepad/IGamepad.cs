// #! csharp
//#r "nuget: SharpDX.XInput, 4.2.0"
//#r "nuget: SharpDX, 4.2.0"
//#r "nuget: SharpDX.DirectInput, 4.2.0"

using System;
using SharpDX.XInput;
using SharpDX.DirectInput;
using System.Linq;

using Rhino;

namespace Daxs
{
    public interface IGamepad
    {
        bool IsConnected { get; }
        GamepadState GetState();
    }

    

    public class PS4Gamepad : IGamepad
    {
        public bool IsConnected => joystick != null;

        private DirectInput _directInput;
        private Joystick joystick;

        public PS4Gamepad()
        {
            _directInput = new DirectInput();

            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            var ps4Device = devices.FirstOrDefault(); // Or filter for specific name/product GUID if needed

            if (ps4Device != null)
            {
                joystick = new Joystick(_directInput, ps4Device.InstanceGuid);
                joystick.Properties.BufferSize = 128;
                try
                {
                    joystick.Acquire();
                }
                catch (SharpDX.SharpDXException ex)
                {
                    Console.WriteLine("Acquire failed: " + ex.Message);
                    joystick = null;
                }
            }
            else
            {
                RhinoApp.WriteLine("ps4Device == null");
            }
        }

        public GamepadState GetState()
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

            GamepadState gState = new GamepadState
            (
                A: buttons[1],
                B: buttons[2],
                X: buttons[0],
                Y: buttons[3],

                Start: buttons[9],
                Back: buttons[8],

                L1: buttons[4],
                L2: (buttons[6]) ? state.RotationX / 65535f : 0,
                L3: buttons[10],

                R1: buttons[5],
                R2: (buttons[7]) ? state.RotationY / 65535f : 0,
                R3: buttons[11],

                DPadUp: pov == 0 || pov == 4500 || pov == 31500,
                DPadDown: pov == 13500 || pov == 18000 || pov == 22500,
                DPadLeft: pov == 18000 || pov == 27000 || pov == 31500,
                DPadRight: pov == 4500 || pov == 9000 || pov == 13500,

                LeftThumbX: (state.X - 32768f) / 32768f,
                LeftThumbY: -(state.Y - 32768f) / 32768f,
                RightThumbX: (state.Z - 32768f) / 32768f,
                RightThumbY: -(state.RotationZ - 32768f) / 32768f
            );

            return gState;
        }
    }
}