namespace Daxs
{
    public interface IGamepadLayout
    {
        Layout Name { get; }
        void HandleInput(Gamepad state, double delta);
    }
}