using SDL3;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static SDL3.SDL;

namespace Daxs.GUI
{
    internal sealed class GamepadOverlayState
    {
        public HashSet<GamepadButton> ActiveButtons { get; } = new();

        public float LeftStickX { get; set; }    
        public float LeftStickY { get; set; } 
        public float RightStickX { get; set; }  
        public float RightStickY { get; set; } 

        public float LeftTrigger { get; set; }
        public float RightTrigger { get; set; }

        public GamepadOverlayState Clone()
        {
            var copy = new GamepadOverlayState
            {
                LeftStickX = LeftStickX,
                LeftStickY = LeftStickY,
                RightStickX = RightStickX,
                RightStickY = RightStickY,
                LeftTrigger = LeftTrigger,
                RightTrigger = RightTrigger
            };

            foreach (var b in ActiveButtons)
                copy.ActiveButtons.Add(b);

            return copy;
        }

        public string BuildVisualKey()
        {
            return string.Join("|",
                string.Join(",", ActiveButtons.OrderBy(x => (int)x).Select(x => x.ToString())),
                LeftStickX.ToString("0.00", CultureInfo.InvariantCulture),
                LeftStickY.ToString("0.00", CultureInfo.InvariantCulture),
                RightStickX.ToString("0.00", CultureInfo.InvariantCulture),
                RightStickY.ToString("0.00", CultureInfo.InvariantCulture),
                LeftTrigger.ToString("0.00", CultureInfo.InvariantCulture),
                RightTrigger.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }
}