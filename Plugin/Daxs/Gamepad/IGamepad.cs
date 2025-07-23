// #! csharp
#r "SharpDX.dll"
#r "SharpDX.XInput.dll"
#r "nuget: SharpDX.DirectInput, 4.2.0"

using System;
using SharpDX;
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

    public readonly struct GamepadState
    {
        public readonly bool A, B, X, Y, Start, Back, L1, L3, R1, R3, DPadUp, DPadDown, DPadLeft, DPadRight;
        public readonly float L2, R2;
        public readonly double LeftThumbX, LeftThumbY, RightThumbX, RightThumbY;

        public GamepadState(bool A, bool B, bool X, bool Y,bool Start, bool Back, bool L1, float L2, 
        bool L3, bool R1, float R2, bool R3, bool DPadUp, bool DPadDown, bool DPadLeft, bool DPadRight,
        double LeftThumbX, double LeftThumbY, double RightThumbX, double RightThumbY)
        {
            this.A = A; this.B = B; this.X = X; this.Y = Y;
            this.Start = Start; this.Back = Back;
            this.L1 = L1; this.L2 = L2; this.L3 = L3;
            this.R1 = R1; this.R2 = R2; this.R3 = R3;
            this.DPadUp = DPadUp; this.DPadDown = DPadDown;
            this.DPadLeft = DPadLeft; this.DPadRight = DPadRight;
            this.LeftThumbX = LeftThumbX; this.LeftThumbY = LeftThumbY;
            this.RightThumbX = RightThumbX; this.RightThumbY = RightThumbY;
        }

        public override string ToString()
        {
            return $"Buttons: A={A}, B={B}, X={X}, Y={Y}, Start={Start}, Back={Back}\n" +
                $"L1={L1}, L3={L3}, R1={R1}, R3={R3}\n" +
                $"DPad: Up={DPadUp}, Down={DPadDown}, Left={DPadLeft}, Right={DPadRight}\n" +
                $"L2={L2:0.00}, R2={R2:0.00}\n" +
                $"Left Thumb: X={LeftThumbX:0.00}, Y={LeftThumbY:0.00}\n" +
                $"Right Thumb: X={RightThumbX:0.00}, Y={RightThumbY:0.00}";
        }
    }

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
                A : (buttons & GamepadButtonFlags.A) != 0,
                B : (buttons & GamepadButtonFlags.B) != 0,
                X : (buttons & GamepadButtonFlags.X) != 0,
                Y : (buttons & GamepadButtonFlags.Y) != 0,

                Start : (buttons & GamepadButtonFlags.Start) != 0,
                Back : (buttons & GamepadButtonFlags.Back) != 0,

                DPadUp : (buttons & GamepadButtonFlags.DPadUp) != 0,
                DPadDown : (buttons & GamepadButtonFlags.DPadDown) != 0,
                DPadLeft : (buttons & GamepadButtonFlags.DPadLeft) != 0,
                DPadRight : (buttons & GamepadButtonFlags.DPadRight) != 0,

                L1 : (buttons & GamepadButtonFlags.LeftShoulder) != 0,
                L2 : pad.LeftTrigger / 255f,
                L3 : (buttons & GamepadButtonFlags.LeftThumb) != 0,

                R1 : (buttons & GamepadButtonFlags.RightShoulder) != 0,
                R2 : pad.RightTrigger / 255f,
                R3 : (buttons & GamepadButtonFlags.RightThumb) != 0,

                LeftThumbX : pad.LeftThumbX/ 32767.0,
                LeftThumbY : pad.LeftThumbY/ 32767.0,
                RightThumbX : pad.RightThumbX/ 32767.0,
                RightThumbY : pad.RightThumbY/ 32767.0
            );
        }
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
            }else
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
                string st= (state == null) ? "null" :state.ToString();

                RhinoApp.WriteLine($"joystick IsDisposed= ${joystick.IsDisposed} || state == ${st}  ");
                return new GamepadState();
            }

            var buttons = state.Buttons;
            var pov = state.PointOfViewControllers.FirstOrDefault();

            GamepadState gState = new GamepadState
            (
                A :  buttons[1],
                B :  buttons[2],         
                X :  buttons[0],
                Y :  buttons[3],

                Start   : buttons[9],
                Back    : buttons[8],

                L1 :  buttons[4],
                L2 : (buttons[6]) ? state.RotationX / 65535f : 0,
                L3 :  buttons[10],

                R1 :  buttons[5],
                R2 : (buttons[7]) ? state.RotationY / 65535f : 0,
                R3 :  buttons[11],

                DPadUp      : pov == 0 || pov == 4500 || pov == 31500,
                DPadDown    : pov == 13500 || pov == 18000 || pov == 22500,
                DPadLeft    : pov == 18000 || pov == 27000 || pov == 31500,
                DPadRight   : pov == 4500 || pov == 9000 || pov == 13500,

                LeftThumbX  :  (state.X - 32768f) / 32768f,
                LeftThumbY  :  -(state.Y - 32768f) / 32768f,
                RightThumbX : (state.Z - 32768f) / 32768f,
                RightThumbY : -(state.RotationZ - 32768f) / 32768f
            );
      
            return gState;
        }
    }
}