using SDL3;
using System;
using static SDL3.SDL;

namespace Daxs
{
    public class GamepadState
    {
        private readonly GamepadButton[] _buttons;
        private readonly bool[] _prevButtons;
        private readonly bool[] _currButtons;
        public readonly InputX[] ButtonStates;

        private readonly GamepadAxis[] _axes;
        private readonly float[] _prevAxes;
        private readonly float[] _currAxes;
        public readonly InputX[] AxisStates;

        private readonly IntPtr _handle;

        private const float AXIS_THRESHOLD = 0.05f;

        public GamepadState(IntPtr handle)
        {
            _handle = handle;

            // 🔹 Buttons
            _buttons = (GamepadButton[])Enum.GetValues(typeof(GamepadButton));
            _prevButtons = new bool[_buttons.Length];
            _currButtons = new bool[_buttons.Length];
            ButtonStates = new InputX[_buttons.Length];

            // 🔹 Axes
            _axes = (GamepadAxis[])Enum.GetValues(typeof(GamepadAxis));
            _prevAxes = new float[_axes.Length];
            _currAxes = new float[_axes.Length];
            AxisStates = new InputX[_axes.Length];
        }

        public void Update()
        {
            if (_handle == IntPtr.Zero || !SDL.GamepadConnected(_handle))
                return;

            // --- BUTTONS ---
            Array.Copy(_currButtons, _prevButtons, _currButtons.Length);
            for (int i = 0; i < _buttons.Length; i++)
                _currButtons[i] = SDL.GetGamepadButton(_handle, _buttons[i]);

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
                float raw = SDL.GetGamepadAxis(_handle, _axes[i]);

                // Normalize
                if (_axes[i] == GamepadAxis.LeftTrigger || _axes[i] == GamepadAxis.RightTrigger)
                    _currAxes[i] = raw / 32767f; // 0..1
                else
                    _currAxes[i] = Math.Clamp(raw / 32767f, -1f, 1f); // -1..1
            }

            for (int i = 0; i < _axes.Length; i++)
            {
                float prev = _prevAxes[i];
                float curr = _currAxes[i];
                bool wasActive = Math.Abs(prev) > AXIS_THRESHOLD;
                bool isActive = Math.Abs(curr) > AXIS_THRESHOLD;

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

        public float GetAxisValue(GamepadAxis a)
        {
            int idx = Array.IndexOf(_axes, a);
            return _currAxes[idx];
        }
    }
}