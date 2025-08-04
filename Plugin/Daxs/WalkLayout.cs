// #! csharp
using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;

namespace Daxs
{
    public class WalkLayout :FlyLayout
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

        public WalkLayout() :base()
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
            double speed = state.L3 ? 3 * moveSpeed : moveSpeed;
            double rotSpeed = state.R3 ? 3 : 1;
            double vertical = GetNonLinearTrigger(state.R2) - GetNonLinearTrigger(state.L2);

            var (yaw, pitch) = NormalizeStickInput(state.RightThumbX, state.RightThumbY);
            var (strafe, forward) = NormalizeStickInput(state.LeftThumbX, state.LeftThumbY);


            bool hasMoved = yaw != 0 || pitch != 0 || forward != 0 || strafe != 0 || Math.Abs(vertical) > 0.02;

            bool r1 = (state.R1 && !prevState.R1);
            bool l1 = (state.L1 && !prevState.L1);

            JumpDir jDir= JumpDir.Default;

            if (r1 && !l1  )
                jDir = JumpDir.Up;
            else if (l1 && !r1  )
                jDir = JumpDir.Down;

            // Update the camera on the UI thread.
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {          
                var view = doc.Views.ActiveView;
                var vp = view.ActiveViewport;   

                if (state.Start && !prevState.Start)
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
                        ApplyCameraControls(vp, forward, -strafe, vertical, yaw, pitch, speed, rotSpeed,jDir);

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
            Vector3d move =  camDir * forward * speed +right * strafe * speed;
            move.Z = vertical;

            Point3d pos = vp.CameraLocation + move;

            //Collision

            if(collider != null)
                GetMeshCollision(ref  pos, collider, jumpDir);
            else
            {
                RhinoApp.WriteLine($"collider == null");
            }

            vp.SetCameraLocation(pos, true);
        }

        private void GetMeshCollision(ref Point3d pos, Mesh colMsh, JumpDir jumpDir)
        {
            Vector3d dir = (jumpDir == JumpDir.Up) ? Vector3d.ZAxis : -Vector3d.ZAxis;           
            Ray3d ray = new Ray3d(pos, dir);

            double distance  = Rhino.Geometry.Intersect.Intersection.MeshRay(colMsh, ray);

            // if(jumpDir == JumpDir.Default)
 
            // {
            //     Point3d[] hits = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, new GeometryBase[]{colMsh},9);

            //     double minDistan
            //     for(int i =0;i< hits.Length; i++)
            //     {
            //         RhinoApp.WriteLine($"hits: " +hits.Length );
            //     }
            // }

            if (distance> 0 && distance<maximalJump)
            {
                pos.Z -= distance - eyeHeight;

            }else if(jumpDir == JumpDir.Down)
            {

            }else
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

        public void ClearCollider()
        {
            collider = null;
        } 
    }
}