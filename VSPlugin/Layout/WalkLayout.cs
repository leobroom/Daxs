// #! csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.Geometry.Intersect;

namespace Daxs
{
    public class WalkLayout : FlyLayout
    {
        protected enum JumpDir
        {
            Default,
            Up,
            Down
        }

        public override string Name => "Walk";

        private Mesh collider = null;

        private double eyeHeight, maximalJump;

        public WalkLayout() : base()
        {
            var eH = settings["EyeHeight"];
            var mj = settings["MaximalJump"];

            eH.ValueChanged += (s, val) => eyeHeight = val;
            mj.ValueChanged += (s, val) => maximalJump = val;

            eyeHeight = eH.Value;
            maximalJump = mj.Value;
        }

        public override void HandleInput(GamepadState state, GamepadState prevState)
        {
            double speed = state.L3 == IInputState.IsHold ? 3 * moveSpeed : moveSpeed;
            double rotSpeed = state.R3 == IInputState.IsHold ? 3 : 1;
            double vertical = GetNonLinearTrigger(state.R2) - GetNonLinearTrigger(state.L2);

            var (yaw, pitch) = NormalizeStickInput(state.RightThumbX, state.RightThumbY);
            var (strafe, forward) = NormalizeStickInput(state.LeftThumbX, state.LeftThumbY);

            bool r1 = (state.R1 == IInputState.IsDown);
            bool l1 = (state.L1 == IInputState.IsDown);

            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02 || r1 || l1;

            JumpDir jDir = JumpDir.Default;

            if (r1)
                jDir = JumpDir.Up;
            else if (l1)
                jDir = JumpDir.Down;

            // Update the camera on the UI thread.
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                var view = doc.Views.ActiveView;
                var vp = view.ActiveViewport;

                if (state.Start == IInputState.IsDown)
                {
                    RhinoApp.WriteLine($"START PRESSED");
                    RhinoApp.RunScript("X_Settings", false);
                    ControllerManager.Instance.SetMessage("Settings");
                }

                if (hasMoved)
                {
                    if (vp.IsPlanView)
                        ApplyCameraPanControls(vp, forward, strafe, vertical, yaw, pitch, speed, rotSpeed);
                    else
                        ApplyCameraControls(vp, forward, -strafe, vertical, yaw, pitch, speed, rotSpeed, jDir);

                    view.Redraw();
                }
            }));
        }

        protected void ApplyCameraControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed, JumpDir jumpDir)
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
            Vector3d move = camDir * forward * speed + right * strafe * speed;
            move.Z = vertical;

            Point3d pos = vp.CameraLocation + move;

            //Collision
            if (collider != null)
                GetMeshCollision(ref pos, collider, jumpDir);

            vp.SetCameraLocation(pos, true);
        }

        private void GetMeshCollision(ref Point3d pos, Mesh colMsh, JumpDir jumpDir)
        {
            Vector3d dir = (jumpDir == JumpDir.Up) ? Vector3d.ZAxis : -Vector3d.ZAxis;
            Ray3d ray = new Ray3d(pos, dir);

            double distance = Rhino.Geometry.Intersect.Intersection.MeshRay(colMsh, ray);

            if (jumpDir == JumpDir.Down || jumpDir == JumpDir.Up)
            {
                Jump(ref pos, colMsh, jumpDir);
            }
            else if (distance > 0 && distance < maximalJump + eyeHeight)
            {
                pos.Z -= distance - eyeHeight;
            }
            else
            {
                pos = colMsh.ClosestPoint(pos);
                pos.Z += eyeHeight;
            }
        }

        public void SetCollider(Mesh collider)
        {
            this.collider = collider;
            RhinoApp.WriteLine("Walk layout - SetCollider.");
        }

        public void ClearCollider() { collider = null; }

        //----------Jump

        private void Jump(ref Point3d pos, Mesh colMsh, JumpDir jumpDir)
        {
            if (jumpDir == JumpDir.Default)
                return;

            RhinoApp.WriteLine($"JUmp: " + jumpDir);

            Ray3d ray = new Ray3d(pos, -Vector3d.ZAxis);
            Point3d[] pts = Intersection.ProjectPointsToMeshes(new Mesh[] { colMsh }, new Point3d[] { pos }, Vector3d.ZAxis, 0.1);

            List<double> lst = new List<double>();

            for (int i = 0; i < pts.Length; i++)
            {
                Point3d pt = pts[i];
                double dist = pt.DistanceTo(pos);

                if (pt.Z > pos.Z)
                {
                    if (jumpDir == JumpDir.Up)
                        lst.Add(dist);
                }
                else if (jumpDir == JumpDir.Down)
                    lst.Add(dist);
            }

            lst.Sort();

            pos.Z += (jumpDir == JumpDir.Up) ? GetNextUp(lst, eyeHeight) : GetNextDown(lst, eyeHeight);
        }

        double GetNextDown(List<double> lst, double eyeHeight)
        {
            for (int i = 1; i < lst.Count; i++)
            {
                double height = lst[i];

                double addedHeight = -height + eyeHeight;
                double dist = height - lst[i - 1];

                if (dist <= eyeHeight)
                    continue;

                return addedHeight;
            }
            return 0;
        }

        double GetNextUp(List<double> lst, double eyeHeight)
        {
            for (int i = 0; i < lst.Count; i++)
            {
                double height = lst[i];
                double addedHeight = height + eyeHeight;

                if (i == lst.Count - 1)
                    return addedHeight;

                double dist = lst[i + 1] - height;

                if (dist <= eyeHeight)
                    continue;

                return addedHeight;
            }
            return 0;
        }
    }
}