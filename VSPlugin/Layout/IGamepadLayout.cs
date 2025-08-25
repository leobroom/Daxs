using System.Diagnostics;

namespace Daxs
{
    /// Abstract Interface to allow different layouts
    public interface IGamepadLayout
    {
        Layout Name { get; }
        double HandleInput(GamepadState state, Stopwatch stopwatch, double lastTime);
    }
}