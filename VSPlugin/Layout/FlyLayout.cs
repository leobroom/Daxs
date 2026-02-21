using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using static SDL3.SDL;

namespace Daxs
{
    internal class FlyLayout : BaseLayout
    {
        public override Layout Name => Layout.Fly;

        public FlyLayout() : base() 
        {
            moveSpeed = settings.BindNumeric("MoveSpeed", v => moveSpeed = v);


            elevateSpeed = settings.BindNumeric("ElevateSpeed", v => elevateSpeed = v);

            hud.Enabled = true;        
        }

        protected double moveSpeed, elevateSpeed;

        protected Plane camPlane;

        double yawAcc = 0.0, pitchAcc = 0.0;
        protected Vector3d zAxis = Vector3d.ZAxis;
        readonly double rad85 = RhinoMath.ToRadians(85);

        public override void HandleInput(Gamepad state)
        {
            // Inputs
            double speedMulti = actionManager.Speedmulti ;   // planar speed multiplier
            double rotSpeedMulti = actionManager.RotSpeedmulti;         // rotation speed multiplier

            if (speedMulti > 1.00)
                hud.SetText("🎮", "Speed X " + speedMulti);

            if (rotSpeedMulti > 1.00)
                hud.SetText("🎮", "Rotation X " + rotSpeedMulti);

            //RhinoApp.WriteLine("TICK / HandleInput");
            double vertical = GetNonLinearTrigger(actionManager.ElevateUp) - GetNonLinearTrigger(actionManager.ElevateDown);

            var (yaw, pitch) = NormalizeStick(state.GetAxisValue(GamepadAxis.RightX), state.GetAxisValue(GamepadAxis.RightY));
            var (strafe, forward) = NormalizeStick(state.GetAxisValue(GamepadAxis.LeftX), state.GetAxisValue(GamepadAxis.LeftY));

            InputY teleport = actionManager.Teleport;

            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02 || teleport != InputY.Default;

            if (!hasMoved && !_uiUpdatePending) //GetActual Campos
            {
                var vp = doc?.Views?.ActiveView?.ActiveViewport;
                if (vp == null)
                    return;

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

            actionManager.QueueActions();

            if ((hasMoved || actionManager.HasActions) && sinceLastUi >= uiDt && !_uiUpdatePending)
            {
                _uiUpdatePending = true;

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    var view = doc.Views.ActiveView;
                    var vp = view.ActiveViewport;

                    actionManager.ExecuteActionsOnMainThread();

                    if (hasMoved) 
                    {
                        if (!vp.IsPlanView)
                        {
                            //RhinoApp.WriteLine("" + vp + " | " + forward + " | " + strafe + " | " + vertical + " | " + pitch + " | " + moveSpeed + " | " + delta);
                            vp.SetCameraLocation(camPlane.Origin, true);
                            vp.SetCameraDirection(camPlane.XAxis, true);
                        }
                        else
                            ApplyCameraPanControls(vp, forward, strafe, vertical, pitch, moveSpeed, delta);

                        view.Redraw();
                    }

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
                        + zAxis * (vertical * elevateSpeed * delta);

            return new Plane(camPlane.Origin + move, fwd, right);
        }

        double GetPitch(double pitchAcc) => Math.Max(-rad85, Math.Min(rad85, pitchAcc));  // Limit

        static double GetNonLinearTrigger(double raw) 
        {
            double normalized = Math.Clamp(raw / MAX_SHORT_VALUE, 0.0, 1.0);
            return Math.Pow(normalized, 2);
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