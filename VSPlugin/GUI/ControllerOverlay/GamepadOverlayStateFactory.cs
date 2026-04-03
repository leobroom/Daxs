using SDL3;
using System;
using static SDL3.SDL;

namespace Daxs.GUI
{
    internal static class GamepadOverlayStateFactory
    {
        public static GamepadOverlayState FromGamepad(Gamepad gamepad)
        {
            var state = new GamepadOverlayState();

            // Buttons
            AddIfActive(gamepad, state, GamepadButton.Back);
            AddIfActive(gamepad, state, GamepadButton.Start);
            AddIfActive(gamepad, state, GamepadButton.Guide);

            AddIfActive(gamepad, state, GamepadButton.DPadUp);
            AddIfActive(gamepad, state, GamepadButton.DPadDown);
            AddIfActive(gamepad, state, GamepadButton.DPadLeft);
            AddIfActive(gamepad, state, GamepadButton.DPadRight);

            AddIfActive(gamepad, state, GamepadButton.South);
            AddIfActive(gamepad, state, GamepadButton.East);
            AddIfActive(gamepad, state, GamepadButton.West);
            AddIfActive(gamepad, state, GamepadButton.North);

            AddIfActive(gamepad, state, GamepadButton.LeftShoulder);
            AddIfActive(gamepad, state, GamepadButton.RightShoulder);

            AddIfActive(gamepad, state, GamepadButton.LeftStick);
            AddIfActive(gamepad, state, GamepadButton.RightStick);

            // Sticks
            state.LeftStickX = NormalizeSigned(gamepad.GetAxisValue(GamepadAxis.LeftX));
            state.LeftStickY = NormalizeSigned(gamepad.GetAxisValue(GamepadAxis.LeftY));
            state.RightStickX = NormalizeSigned(gamepad.GetAxisValue(GamepadAxis.RightX));
            state.RightStickY = NormalizeSigned(gamepad.GetAxisValue(GamepadAxis.RightY));

            // Triggers
            state.LeftTrigger = NormalizeUnsigned(gamepad.GetAxisValue(GamepadAxis.LeftTrigger));
            state.RightTrigger = NormalizeUnsigned(gamepad.GetAxisValue(GamepadAxis.RightTrigger));

            return state;
        }

        private static void AddIfActive(Gamepad gamepad, GamepadOverlayState state, GamepadButton button)
        {
            if (gamepad.GetButtonState(button) != InputX.IsUnset)
                state.ActiveButtons.Add(button);
        }

        private static float NormalizeSigned(short value)
        {
            float v = value / (float)short.MaxValue;
            return Math.Clamp(v, -1f, 1f);
        }

        private static float NormalizeUnsigned(short value)
        {
            float v = value / (float)short.MaxValue;
            return Math.Clamp(v, 0f, 1f);
        }
    }
}