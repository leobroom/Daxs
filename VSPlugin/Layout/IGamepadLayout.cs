using System.Diagnostics;

namespace Daxs
{
    /// Abstract Interface to allow different layouts
    public interface IGamepadLayout
    {
        Layout Name { get; }
        void HandleInput(GamepadState state, double delta);
    }
}