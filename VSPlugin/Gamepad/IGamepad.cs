namespace Daxs
{
    public interface IGamepad
    {
        bool IsConnected { get; }
        GamepadState GetState();
    }

    public abstract class Gamepad : IGamepad
    {
        public abstract bool IsConnected { get; }

        protected GamepadState current;
        protected GamepadState previous = new();

        public abstract GamepadState GetState();

        protected static InputX GetInputState(bool input, InputX previous)
        {
            if (input && (previous == InputX.IsUnset || previous == InputX.IsReleased))
                return InputX.IsDown;
            else if (input && (previous == InputX.IsDown || previous == InputX.IsHold))
                return InputX.IsHold;
            else if (!input && (previous == InputX.IsDown || previous == InputX.IsHold))
                return InputX.IsReleased;
            else
                return InputX.IsUnset;
        }

        internal static IGamepad TryGetGamepad()
        {
            var xbox = new XboxGamepad();
            if (xbox.IsConnected)
                return xbox;
            else
            {
                var ps4 = new PS4Gamepad();
                if (ps4.IsConnected)
                    return ps4;
            }

            return null;
        }
    }
}