using Daxs.Actions;
using Daxs.GUI;
using Daxs.Settings;
using Rhino;
using System;
using static SDL3.SDL;

namespace Daxs.Layout
{
    internal abstract class BaseLayout : IGamepadLayout
    {
        protected double delta = 0;

        // UI throttle accumulator and target interval (~60 FPS)
        protected double sinceLastUi = 0.0;
        protected const double uiDt = 1.0 / 60.0;
        protected volatile bool _uiUpdatePending = false;

        protected const double MAX_SHORT_VALUE = 32767.0;

        public abstract LayoutType Name { get; }

        protected readonly RhinoDoc doc = RhinoDoc.ActiveDoc;
        protected readonly DaxsConfig settings = DaxsConfig.Instance;
        protected readonly ActionDispatcher actionManager = ActionDispatcher.Instance;
        protected readonly HUD hud = HUD.Instance;

        protected double deadzone, yawSensitivity, pitchSensitivity, speedFactor;

        public BaseLayout() 
        {
            deadzone = settings.BindNumeric("Deadzone", v => deadzone = v);
            yawSensitivity = settings.BindNumeric("YawSensitivity", v => yawSensitivity = v);
            pitchSensitivity = settings.BindNumeric("PitchSensitivity", v => pitchSensitivity = v);
            speedFactor = settings.BindNumeric("SpeedFactor", v => speedFactor = v);
        }

        public abstract void HandleInput(Gamepad state);

        public void HandleInputAndDelta(Gamepad state, double delta)
        {
            this.delta = delta;
            sinceLastUi += delta;   //  accumulate time for UI throttling
            HandleInput(state);
        }

        /// <summary>
        /// Normalize Stick value and applies deadzone to it
        /// </summary>
        protected (double x, double y) NormalizeStick(double nx, double ny)
        {
            nx = nx / MAX_SHORT_VALUE;
            ny = ny / MAX_SHORT_VALUE;

            nx = Math.Clamp(nx, -1.0, 1.0);
            ny = Math.Clamp(ny, -1.0, 1.0);

            double r2 = nx * nx + ny * ny;
            double dz2 = deadzone * deadzone;

            if (r2 <= dz2)
                return (0, 0);

            double r = Math.Sqrt(r2);
            double invR = 1.0 / r;

            // direction
            double dirX = -nx * invR;
            double dirY = -ny * invR;

            // scale from deadzone to 1
            double scale = (r - deadzone) * (1.0 / Math.Max(1e-6, 1.0 - deadzone));

            scale = Math.Pow(scale, 2);

            double yaw = Math.Clamp(dirX * scale, -1.0, 1.0);
            double pitch = Math.Clamp(dirY * scale, -1.0, 1.0);

            return (yaw, pitch);
        }
    }
}