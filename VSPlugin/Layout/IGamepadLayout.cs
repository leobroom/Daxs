namespace Daxs
{
    public interface IGamepadLayout
    {
        Layout Name { get; }
        void HandleInput(Gamepad state);
        void HandleInputAndDelta(Gamepad gamepad, double delta);
    }
}