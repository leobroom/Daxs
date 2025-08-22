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
        protected GamepadState previous = new GamepadState();

        public abstract GamepadState GetState();

        protected static IInputState GetInputState(bool input, IInputState previous)
        {
            if (input && (previous == IInputState.IsUnset || previous == IInputState.IsReleased))
                return IInputState.IsDown;
            else if (input && (previous == IInputState.IsDown || previous == IInputState.IsHold))
                return IInputState.IsHold;
            else if (!input && (previous == IInputState.IsDown || previous == IInputState.IsHold))
                return IInputState.IsReleased;
            else
                return IInputState.IsUnset;
        }
    }

    public enum IInputState
    {
        IsUnset,
        IsDown,
        IsHold,
        IsReleased
    }
}