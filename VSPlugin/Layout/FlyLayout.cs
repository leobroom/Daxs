// #! csharp
using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using System.Diagnostics;

namespace Daxs
{
    public class FlyLayout : IGamepadLayout
    {
        public virtual string Name => "Fly";
        protected double moveSpeed, deadzone, yawSensitivity, pitchSensitivity;
        protected RhinoDoc doc = RhinoDoc.ActiveDoc;
        protected Settings settings;

        public FlyLayout()
        {
            settings = Settings.Instance;

            var mV = settings["MoveSpeed"];
            var dZ = settings["Deadzone"];
            var yS = settings["YawSensitivity"];
            var pS = settings["PitchSensitivity"];

            mV.ValueChanged += (s, val) => moveSpeed = val;
            dZ.ValueChanged += (s, val) => deadzone = val;
            yS.ValueChanged += (s, val) => yawSensitivity = val;
            pS.ValueChanged += (s, val) => pitchSensitivity = val;

            moveSpeed = mV.Value;
            deadzone = dZ.Value;
            yawSensitivity = yS.Value;
            pitchSensitivity = pS.Value;
        }

        public virtual double HandleInput(GamepadState state, Stopwatch stopwatch, double lastTime)
        {
            double speed = state.L3 == IInputState.IsHold ? 3 * moveSpeed : moveSpeed;
            double rotSpeed = state.R3 == IInputState.IsHold ? 3 : 1;
            double vertical = GetNonLinearTrigger(state.R2) - GetNonLinearTrigger(state.L2);

            var (yaw, pitch) = NormalizeStickInput(state.RightThumbX, state.RightThumbY);
            var (strafe, forward) = NormalizeStickInput(state.LeftThumbX, state.LeftThumbY);

            //bool hasBooster = state.L3 || state.R3;

            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02;

            // Update the camera on the UI thread.
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                var view = doc.Views.ActiveView;
                var vp = view.ActiveViewport;

                double currentTime = stopwatch.Elapsed.TotalSeconds;
                float deltaTime = (float)(currentTime - lastTime);
                lastTime = currentTime;

                if (state.Start == IInputState.IsDown)
                {
                    RhinoApp.RunScript("Daxs_Settings", false);
                }

                if (hasMoved)
                {
                    if (vp.IsPlanView)
                        ApplyCameraPanControls(vp, forward, strafe, vertical, yaw, pitch, speed, rotSpeed);
                    else
                        ApplyCameraControls(vp, forward, -strafe, vertical, yaw, pitch, speed, rotSpeed);

                    view.Redraw();
                }
            }));

            return lastTime;
        }

        protected (double x, double y) NormalizeStickInput(double normX, double normY)
        {
            double magnitude = Math.Sqrt(normX * normX + normY * normY);
            if (magnitude < deadzone)
                return (0, 0);

            double scaled = Math.Clamp((magnitude - deadzone) / (1 - deadzone), 0, 1);

            // Normalize direction
            double dirX = normX / magnitude;
            double dirY = normY / magnitude;

            // Apply sensitivity and scale
            double yaw = -dirX * scaled * yawSensitivity;
            double pitch = dirY * scaled * pitchSensitivity;

            return (yaw, pitch);
        }

        /// Used for panning over a plan views (example left right bottom etc)
        protected void ApplyCameraPanControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed)
        {
            // Get the right and up vectors in the view plane
            Vector3d right = -vp.CameraX;
            Vector3d up = vp.CameraY;

            // Movement in the view plane
            Vector3d move = (right * strafe + up * forward) * speed;

            // Optionally allow movement along the world Z axis
            move += Vector3d.ZAxis * vertical * speed;

            // Apply movement to both camera and target
            vp.SetCameraLocation(vp.CameraLocation + move, true);
            vp.SetCameraTarget(vp.CameraTarget + move, true);

            if (Math.Abs(pitch) > 1e-6)
            {
                // Sensible scaling factor: pitch > 0 zooms in; < 0 zooms out
                double scale = Math.Pow(1.1, pitch * speed); // invert pitch for natural stick direction
                vp.Magnify(scale, false);
            }
        }

        protected virtual void ApplyCameraControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed)
        {
            Vector3d camDir = vp.CameraDirection;

            // Rotation: Yaw around world Z
            camDir.Transform(Transform.Rotation(yaw * rotSpeed, Vector3d.ZAxis, Point3d.Origin));

            // Rotation: Pitch around right vector
            Vector3d right = Vector3d.CrossProduct(camDir, Vector3d.ZAxis);

            camDir.Transform(Transform.Rotation(pitch * rotSpeed, right, Point3d.Origin));
            vp.SetCameraDirection(camDir, true);

            // Recalculate right vector after rotation
            right = Vector3d.CrossProduct(camDir, vp.CameraUp);

            // Movement
            Vector3d move = Vector3d.ZAxis * vertical +
                            camDir * forward * speed +
                            right * strafe * speed;

            vp.SetCameraLocation(vp.CameraLocation + move, true);
        }

        protected double GetNonLinearTrigger(double raw) => Math.Pow(raw, 2); // quadratic curve 
    }
}