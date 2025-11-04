using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using static SDL3.SDL;

namespace Daxs
{
    internal class FlyLayout : IGamepadLayout
    {
        public virtual Layout Name => Layout.Fly;
        protected double moveSpeed, deadzone, yawSensitivity, pitchSensitivity, elevateSpeed;
        protected RhinoDoc doc = RhinoDoc.ActiveDoc;
        protected Settings settings;
        protected ActionManager actionManager = ActionManager.Instance;

        protected readonly HUD hud = HUD.Instance;

        public FlyLayout()
        {
            settings = Settings.Instance;

            moveSpeed = settings.BindNumeric("MoveSpeed", v => moveSpeed = v);
            deadzone = settings.BindNumeric("Deadzone", v => deadzone = v);
            yawSensitivity = settings.BindNumeric("YawSensitivity", v => yawSensitivity = v);
            pitchSensitivity = settings.BindNumeric("PitchSensitivity", v => pitchSensitivity = v);
            elevateSpeed = settings.BindNumeric("ElevateSpeed", v => elevateSpeed = v);

            hud.Enabled = true;        
        }

        // fields
        volatile bool _uiUpdatePending = false;
        protected Plane camPlane;

        // fields
        double yawAcc = 0.0, pitchAcc = 0.0;
        protected Vector3d zAxis = Vector3d.ZAxis;
        readonly double rad85 = RhinoMath.ToRadians(89);

        // UI throttle accumulator and target interval (~60 FPS)
        double sinceLastUi = 0.0;
        const double uiDt = 1.0 / 60.0;

        public void HandleInput(GamepadState state, double delta)
        {
            sinceLastUi += delta;   //  accumulate time for UI throttling

            // Inputs
            double speedMulti = actionManager.Speedmulti ;   // planar speed multiplier
            double rotSpeedMulti = actionManager.RotSpeedmulti;         // rotation speed multiplier

            if (speedMulti > 1.00)
                hud.SetText("Speed X " + speedMulti, 2000);

            if (rotSpeedMulti > 1.00)
                hud.SetText("Rotation X " + rotSpeedMulti, 2000);

            //RhinoApp.WriteLine("TICK / HandleInput");

            double vertical = GetNonLinearTrigger(actionManager.ElevateUp) - GetNonLinearTrigger(actionManager.ElevateDown);


            var (yaw, pitch) = NormalizeStick(state.GetAxisValue(GamepadAxis.RightX), state.GetAxisValue(GamepadAxis.RightY));
            var (strafe, forward) = NormalizeStick(state.GetAxisValue(GamepadAxis.LeftX), state.GetAxisValue(GamepadAxis.LeftY));

            InputY teleport = actionManager.Teleport;

            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02 || teleport != InputY.Default;

            if (!hasMoved && !_uiUpdatePending) //GetActual Campos
            {
                var vp = doc.Views.ActiveView.ActiveViewport;

                Vector3d camDir = vp.CameraDirection; // Read current viewport camera
                Vector3d right = Vector3d.CrossProduct(zAxis, camDir); // Turntable basis (use world up to remove roll)

                if (!right.Unitize())
                    right = Vector3d.XAxis; // guard near poles

                camPlane = new Plane(vp.CameraLocation, camDir, right);

                // Rebase angle accumulators to match viewport direction
                double newYaw = Math.Atan2(camDir.Y, camDir.X);
                double newPitch = Math.Asin(camDir.Z);

                pitchAcc = GetPitch(newPitch);
                yawAcc = newYaw;
            }

            if (hasMoved)
            {
                yawAcc += yaw * yawSensitivity * delta * rotSpeedMulti;
                pitchAcc += pitch * pitchSensitivity * delta * rotSpeedMulti;
                pitchAcc = GetPitch(pitchAcc);

                // Rebuild basis from yaw/pitch (turntable, world-up = +Z)
                double cy = Math.Cos(yawAcc);
                double sy = Math.Sin(yawAcc);
                double cp = Math.Cos(pitchAcc);
                double sp = Math.Sin(pitchAcc);

                camPlane = CalculateCamPlane(cp, cy, sy, sp, forward, strafe, vertical, speedMulti * moveSpeed, delta, teleport);
            }

            bool hasAction = actionManager.HasActionsOnMainThread();
            if (hasAction)
            {
                RhinoApp.InvokeOnUiThread((Action)(() => { actionManager.ExecuteActionsOnMainThread(); }));
            }

            if (hasMoved && sinceLastUi >= uiDt && !_uiUpdatePending)
            {
                _uiUpdatePending = true;

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    var view = doc.Views.ActiveView;
                    var vp = view.ActiveViewport;

                    if (!vp.IsPlanView)
                    {
                        //RhinoApp.WriteLine("" + vp + " | " + forward + " | " + strafe + " | " + vertical + " | " + pitch + " | " + moveSpeed + " | " + delta);
                        vp.SetCameraLocation(camPlane.Origin, true);
                        vp.SetCameraDirection(camPlane.XAxis, true);
                    }
                    else
                        ApplyCameraPanControls(vp, forward, strafe, vertical, pitch, moveSpeed, delta);

                    view.Redraw();

                    _uiUpdatePending = false;
                }));
            }
        }

        protected virtual Plane CalculateCamPlane(double cp , double cy, double sy, double sp, double forward, double strafe, double vertical, double speedMulti, double delta, InputY teleport ) 
        {
            var fwd = new Vector3d(cp * cy, cp * sy, sp);
            var right = new Vector3d(-sy, cy, 0);

            Vector3d move = fwd * (forward * speedMulti * delta)
                        + right * (strafe * speedMulti * delta)
                        + zAxis * (vertical * speedMulti * delta);

            return new Plane(camPlane.Origin + move, fwd, right);
        }

        double GetPitch(double pitchAcc) => Math.Max(-rad85, Math.Min(rad85, pitchAcc));  // Limit

        static double GetNonLinearTrigger(float raw) => Math.Pow(raw, 2); // quadratic curve 

        protected (double x, double y) NormalizeStick(double nx, double ny)
        {
            double r2 = nx * nx + ny * ny;
            double dz = deadzone;
            double dz2 = dz * dz;

            if (r2 <= dz2)
                return (0, 0);

            double r = Math.Sqrt(r2);
            double invR = 1.0 / r;

            // direction
            double dirX = nx * invR;
            double dirY = ny * invR;

            // scale from deadzone to 1
            double scale = (r - dz) * (1.0 / Math.Max(1e-6, 1.0 - dz));

            double yaw = -dirX * scale * yawSensitivity;
            double pitch = dirY * scale * pitchSensitivity;

            return (yaw, pitch);
        }

        /// Used for panning over a plan views (example left right bottom etc)
        protected static void ApplyCameraPanControls(RhinoViewport vp, double forward, double strafe, double vertical, double pitch, double speed, double delta)
        {
            // Movement in the view plane
            Vector3d move = ((-vp.CameraX) * strafe * delta + vp.CameraY * forward * delta) * speed;

            // Optionally allow movement along the world Z axis
            move += Vector3d.ZAxis * vertical * speed;

            // Apply movement to both camera and target
            vp.SetCameraLocation(vp.CameraLocation + move, true);
            vp.SetCameraTarget(vp.CameraTarget + move, true);

            if (Math.Abs(pitch) > 1e-6)
            {
                // Sensible scaling factor: pitch > 0 zooms in; < 0 zooms out
                double scale = Math.Pow(1.1, pitch * speed * delta);
                vp.Magnify(scale, false);
            }
        }
    }
}