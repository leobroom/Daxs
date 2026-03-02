//using SDL3;
//using System;
//using static SDL3.SDL;

//namespace Daxs
//{
//    // Minimal payload for EventHandler
//    public sealed class GamepadEventArgs : EventArgs
//    {
//        public Gamepad Gamepad { get; }
//        public GamepadEventArgs(Gamepad gamepad) => Gamepad = gamepad;
//    }

//    public class Gamepad : IDisposable
//    {
//        private bool _disposed;

//        private readonly GamepadButton[] _buttons;
//        private readonly bool[] _prevButtons;
//        private readonly bool[] _currButtons;
//        public readonly InputX[] ButtonStates;

//        private readonly GamepadAxis[] _axes;
//        private readonly short[] _prevAxes;
//        private readonly short[] _currAxes;
//        public readonly InputX[] AxisStates;


//        public IntPtr GamepadID { get; }

//        private const float AXIS_THRESHOLD = 0.05f;

//        //SDL INFO
//        public string Name { get; }
//        public string VendorID { get; }
//        public string ProductID { get; }
//        public string GpType { get; }

//        //Events
//        public static event EventHandler<GamepadEventArgs> Created;
//        public static event EventHandler<GamepadEventArgs> Destroyed;

//        //Current

//        public Gamepad(uint idx)
//        {
//            this.GamepadID = SDL.OpenGamepad(idx);

//            // 🔹 Buttons
//            _buttons = (GamepadButton[])Enum.GetValues(typeof(GamepadButton));
//            _prevButtons = new bool[_buttons.Length];
//            _currButtons = new bool[_buttons.Length];
//            ButtonStates = new InputX[_buttons.Length];

//            // 🔹 Axes
//            _axes = (GamepadAxis[])Enum.GetValues(typeof(GamepadAxis));
//            _prevAxes = new short[_axes.Length];
//            _currAxes = new short[_axes.Length];
//            AxisStates = new InputX[_axes.Length];

//            //
//            Name = SDL.GetGamepadName(GamepadID);
//            VendorID = SDL.GetGamepadVendor(GamepadID).ToString("X4");
//            ProductID = SDL.GetGamepadProduct(GamepadID).ToString("X4");
//            GpType = SDL.GetGamepadType(GamepadID).ToString();

//            Created?.Invoke(this, new GamepadEventArgs(this));
//        }

//        public void Update()
//        {
//            if (_disposed || GamepadID == IntPtr.Zero || !SDL.GamepadConnected(GamepadID))
//                return;

//            // --- BUTTONS ---
//            Array.Copy(_currButtons, _prevButtons, _currButtons.Length);
//            for (int i = 0; i < _buttons.Length; i++)
//                _currButtons[i] = SDL.GetGamepadButton(GamepadID, _buttons[i]);

//            for (int i = 0; i < _buttons.Length; i++)
//            {
//                bool was = _prevButtons[i];
//                bool now = _currButtons[i];

//                if (!was && now)
//                    ButtonStates[i] = InputX.IsDown;
//                else if (was && now)
//                    ButtonStates[i] = InputX.IsHold;
//                else if (was && !now)
//                    ButtonStates[i] = InputX.IsReleased;
//                else
//                    ButtonStates[i] = InputX.IsUnset;
//            }

//            // --- AXES ---
//            Array.Copy(_currAxes, _prevAxes, _currAxes.Length);

//            for (int i = 0; i < _axes.Length; i++)
//                _currAxes[i] = SDL.GetGamepadAxis(GamepadID, _axes[i]);

//            for (int i = 0; i < _axes.Length; i++)
//            {
//                short prev = _prevAxes[i];
//                short curr = _currAxes[i];
//                bool wasActive = Math.Abs((int)prev) > AXIS_THRESHOLD;
//                bool isActive = Math.Abs((int)curr) > AXIS_THRESHOLD;

//                if (!wasActive && isActive)
//                    AxisStates[i] = InputX.IsDown;
//                else if (wasActive && isActive)
//                    AxisStates[i] = InputX.IsHold;
//                else if (wasActive && !isActive)
//                    AxisStates[i] = InputX.IsReleased;
//                else
//                    AxisStates[i] = InputX.IsUnset;
//            }
//        }

//        public InputX GetButtonState(GamepadButton b)
//        {
//            int idx = Array.IndexOf(_buttons, b);
//            return ButtonStates[idx];
//        }

//        public InputX GetAxisState(GamepadAxis a)
//        {
//            int idx = Array.IndexOf(_axes, a);
//            return AxisStates[idx];
//        }

//        public short GetAxisValue(GamepadAxis a)
//        {
//            int idx = Array.IndexOf(_axes, a);
//            return _currAxes[idx];
//        }

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        public bool HasGamepadButton(GamepadButton button) 
//        {
//            return SDL.GamepadHasButton(GamepadID, button);
//        }

//        public string GetButtonLabel(GamepadButton button) 
//        { 
//            return SDL.GetGamepadButtonLabel(GamepadID, button).ToString();
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (_disposed) 
//                return;
//            _disposed = true;

//            if (GamepadID != IntPtr.Zero && SDL.GamepadConnected(GamepadID))
//                SDL.CloseGamepad(GamepadID);

//            Destroyed?.Invoke(this, new GamepadEventArgs(null));
//        }

//        ~Gamepad() => Dispose(false);
//    }
//}










using Daxs.Settings;
using SDL3;
using System;
using static SDL3.SDL;

namespace Daxs
{
    public sealed class GamepadEventArgs : EventArgs
    {
        public Gamepad Gamepad { get; }
        public GamepadEventArgs(Gamepad gamepad) => Gamepad = gamepad;
    }

    public class Gamepad : IDisposable
    {
        private bool _disposed;

        private readonly GamepadButton[] _buttons;
        private readonly bool[] _prevButtons;
        private readonly bool[] _currButtons;
        public InputX[] ButtonStates { get; }

        private readonly GamepadAxis[] _axes;
        private readonly short[] _prevAxes;
        private readonly short[] _currAxes;
        public InputX[] AxisStates { get; }


        public IntPtr GamepadID { get; }

        private int _axisDeadzone;
        private double _deadzone;

        //SDL INFO
        public string Name { get; }
        public string VendorID { get; }
        public string ProductID { get; }
        public string GpType { get; }

        //Events
        public static event EventHandler<GamepadEventArgs> Created;
        public static event EventHandler<GamepadEventArgs> Destroyed;

        //Current
        public Gamepad(uint idx)
        {
            _deadzone = DaxsConfig.Instance.BindNumeric("Deadzone", v => UpdateDeadzone(v));

            // initialize once
            UpdateDeadzone(_deadzone);

            this.GamepadID = SDL.OpenGamepad(idx);

            // 🔹 Buttons
            _buttons = (GamepadButton[])Enum.GetValues(typeof(GamepadButton));
            _prevButtons = new bool[_buttons.Length];
            _currButtons = new bool[_buttons.Length];
            ButtonStates = new InputX[_buttons.Length];

            // 🔹 Axes
            _axes = (GamepadAxis[])Enum.GetValues(typeof(GamepadAxis));
            _prevAxes = new short[_axes.Length];
            _currAxes = new short[_axes.Length];
            AxisStates = new InputX[_axes.Length];

            Name = SDL.GetGamepadName(GamepadID);
            VendorID = SDL.GetGamepadVendor(GamepadID).ToString("X4");
            ProductID = SDL.GetGamepadProduct(GamepadID).ToString("X4");
            GpType = SDL.GetGamepadType(GamepadID).ToString();

            Created?.Invoke(this, new GamepadEventArgs(this));
        }

        private void UpdateDeadzone(double dz)
        {
            dz = Math.Clamp(dz, 0.0, 0.95);
            _deadzone = dz;
            _axisDeadzone = (int)Math.Round(dz * short.MaxValue);
        }

        public void Update()
        {
            if (_disposed || GamepadID == IntPtr.Zero || !SDL.GamepadConnected(GamepadID))
                return;

            // --- BUTTONS ---
            Array.Copy(_currButtons, _prevButtons, _currButtons.Length);
            for (int i = 0; i < _buttons.Length; i++)
                _currButtons[i] = SDL.GetGamepadButton(GamepadID, _buttons[i]);

            for (int i = 0; i < _buttons.Length; i++)
            {
                bool was = _prevButtons[i];
                bool now = _currButtons[i];

                if (!was && now)
                    ButtonStates[i] = InputX.IsDown;
                else if (was && now)
                    ButtonStates[i] = InputX.IsHold;
                else if (was && !now)
                    ButtonStates[i] = InputX.IsReleased;
                else
                    ButtonStates[i] = InputX.IsUnset;
            }

            // --- AXES ---
            Array.Copy(_currAxes, _prevAxes, _currAxes.Length);

            for (int i = 0; i < _axes.Length; i++)
            {
                short raw = SDL.GetGamepadAxis(GamepadID, _axes[i]);
                _currAxes[i] = ApplyDeadzone(raw);
            }

            for (int i = 0; i < _axes.Length; i++)
            {
                short prev = _prevAxes[i];
                short curr = _currAxes[i];
                bool wasActive = Math.Abs((int)prev) > _axisDeadzone;
                bool isActive = Math.Abs((int)curr) > _axisDeadzone;

                if (!wasActive && isActive)
                    AxisStates[i] = InputX.IsDown;
                else if (wasActive && isActive)
                    AxisStates[i] = InputX.IsHold;
                else if (wasActive && !isActive)
                    AxisStates[i] = InputX.IsReleased;
                else
                    AxisStates[i] = InputX.IsUnset;
            }
        }

        private short ApplyDeadzone(short v)
        {
            if (Math.Abs((int)v) <= _axisDeadzone)
                return 0;
            return v;
        }

        public InputX GetButtonState(GamepadButton b)
        {
            int idx = Array.IndexOf(_buttons, b);
            return ButtonStates[idx];
        }

        public InputX GetAxisState(GamepadAxis a)
        {
            int idx = Array.IndexOf(_axes, a);
            return AxisStates[idx];
        }

        public short GetAxisValue(GamepadAxis a)
        {
            int idx = Array.IndexOf(_axes, a);
            return _currAxes[idx];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool HasGamepadButton(GamepadButton button) => SDL.GamepadHasButton(GamepadID, button);

        public string GetButtonLabel(GamepadButton button) => SDL.GetGamepadButtonLabel(GamepadID, button).ToString();

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (GamepadID != IntPtr.Zero && SDL.GamepadConnected(GamepadID))
                SDL.CloseGamepad(GamepadID);

            Destroyed?.Invoke(this, new GamepadEventArgs(null));
        }

        ~Gamepad() => Dispose(false);
    }
}