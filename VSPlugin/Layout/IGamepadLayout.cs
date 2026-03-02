namespace Daxs.Layout
{
    public interface IGamepadLayout
    {
        LayoutType Name { get; }
        void HandleInput(Gamepad state);
        void HandleInputAndDelta(Gamepad gamepad, double delta);
    }
}