// #! csharp
using System;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;

namespace Daxs
{
    public class WalkLayout :FlyLayout
    {
        public override string Name => "Walk";

        private Mesh collider = null;

        private double eyeHeight = 1.70;

        public WalkLayout() :base()
        {
            var eH = settings["EyeHeight"];
            
            eH.ValueChanged += (s, val) => eyeHeight = val;

            eyeHeight = eH.Value;
        }

        protected override void ApplyCameraControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed)
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
                GetMeshCollision(ref  pos, collider);

            vp.SetCameraLocation(pos, true);
        }

        public void GetMeshCollision(ref Point3d pos, Mesh colMsh)
        {
            Ray3d ray = new Ray3d(pos, -Vector3d.ZAxis);
            double distance  = Rhino.Geometry.Intersect.Intersection.MeshRay(colMsh, ray);

            if (distance> 0)
            {
                pos.Z -= distance - eyeHeight;
            }else
            {
                pos = colMsh.ClosestPoint(pos);
                pos.Z += eyeHeight;
            }
        }

        public void SetCollider(Mesh collider){this.collider = collider;}
        public void ClearCollider()
        {
            collider = null;
        } 
    }
}