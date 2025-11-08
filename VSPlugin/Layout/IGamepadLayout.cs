namespace Daxs
{
    public interface IGamepadLayout
    {
        Layout Name { get; }
        void HandleInput(GamepadState state, double delta);
    }
}