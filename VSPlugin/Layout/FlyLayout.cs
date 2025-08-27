using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using System.Diagnostics;

namespace Daxs
{
    internal class FlyLayout : IGamepadLayout
    {
        public virtual Layout Name => Layout.Fly;
        protected double moveSpeed, deadzone, yawSensitivity, pitchSensitivity, elevateSpeed;
        protected RhinoDoc doc = RhinoDoc.ActiveDoc;
        protected Settings settings;
        protected ActionManager actionManager = ActionManager.Instance;

        public FlyLayout()
        {
            settings = Settings.Instance;

            var mV = settings["MoveSpeed"];
            var dZ = settings["Deadzone"];
            var yS = settings["YawSensitivity"];
            var pS = settings["PitchSensitivity"];
            var eS = settings["ElevateSpeed"];

            mV.ValueChanged += (s, val) => moveSpeed = val;
            dZ.ValueChanged += (s, val) => deadzone = val;
            yS.ValueChanged += (s, val) => yawSensitivity = val;
            pS.ValueChanged += (s, val) => pitchSensitivity = val;
            eS.ValueChanged += (s, val) => elevateSpeed = val;

            moveSpeed = mV.Value;
            deadzone = dZ.Value;
            yawSensitivity = yS.Value;
            pitchSensitivity = pS.Value;
            elevateSpeed = eS.Value;
        }

        volatile bool _uiUpdatePending = false;

        public virtual double HandleInput(GamepadState state, Stopwatch stopwatch, double lastTime)
        {
            double speedMulti = actionManager.Speedmulti * moveSpeed;
            double rotSpeedMulti = actionManager.RotSpeedmulti;
            double vertical = GetNonLinearTrigger(actionManager.ElevateUp) - GetNonLinearTrigger(actionManager.ElevateDown);

            var (yaw, pitch) = NormalizeStick(state.RightThumbX, state.RightThumbY);
            var (strafe, forward) = NormalizeStick(state.LeftThumbX, state.LeftThumbY);

            InputY teleport = actionManager.Teleport;

            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02 || teleport != InputY.Default;


                if (hasMoved && !_uiUpdatePending)
            {
                _uiUpdatePending = true;
                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    var view = doc.Views.ActiveView;
                    var vp = view.ActiveViewport;

                    actionManager.ExecuteActionsOnMainThread();

                    double currentTime = stopwatch.Elapsed.TotalSeconds;
                    float delta = (float)(currentTime - lastTime);
                    lastTime = currentTime;

                    if (hasMoved)
                    {
                        if (vp.IsPlanView)
                            ApplyCameraPanControls(vp, forward, strafe, vertical * delta * elevateSpeed, pitch, speedMulti * delta);
                        else
                            ApplyCameraControls(vp, forward, -strafe, vertical * delta * elevateSpeed, yaw, pitch, speedMulti * delta, rotSpeedMulti * delta, teleport);
                    }

                    view.Redraw();
                    _uiUpdatePending = false;


                }));
            }

            return lastTime;
        }

        /// <summary>
        /// Normalize stick input
        /// </summary>
        protected (double x, double y) NormalizeStick(double normX, double normY)
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
        protected static void ApplyCameraPanControls(RhinoViewport vp, double forward, double strafe, double vertical, double pitch, double speed)
        {
            // Movement in the view plane
            Vector3d move = ((-vp.CameraX) * strafe + vp.CameraY * forward) * speed;

            // Optionally allow movement along the world Z axis
            move += Vector3d.ZAxis * vertical * speed;

            // Apply movement to both camera and target
            vp.SetCameraLocation(vp.CameraLocation + move, true);
            vp.SetCameraTarget(vp.CameraTarget + move, true);

            if (Math.Abs(pitch) > 1e-6)
            {
                // Sensible scaling factor: pitch > 0 zooms in; < 0 zooms out
                double scale = Math.Pow(1.1, pitch * speed); 
                vp.Magnify(scale, false);
            }
        }

        protected virtual void ApplyCameraControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed, InputY teleport)
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
            Vector3d move = Vector3d.ZAxis * vertical +camDir * forward * speed +right * strafe * speed;

            vp.SetCameraLocation(vp.CameraLocation + move, true);
        }

        protected static double GetNonLinearTrigger(double raw) => Math.Pow(raw, 2); // quadratic curve 
    }
}