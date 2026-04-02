using SDL3;
using System.Collections.Generic;
using System.Linq;
using static SDL3.SDL;

namespace Daxs.GUI
{
    internal sealed class GamepadOverlayState
    {
        public HashSet<GamepadButton> ActiveButtons { get; } = new();

        public GamepadOverlayState Clone()
        {
            var copy = new GamepadOverlayState();
            foreach (var b in ActiveButtons)
                copy.ActiveButtons.Add(b);
            return copy;
        }

        public string BuildVisualKey()
        {
            return string.Join("|", ActiveButtons.OrderBy(x => (int)x).Select(x => x.ToString()));
        }
    }
}