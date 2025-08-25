using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.Geometry.Intersect;

namespace Daxs
{
    internal class WalkLayout : FlyLayout
    {
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

        protected override void ApplyCameraControls(RhinoViewport vp, double forward, double strafe, double vertical, double yaw, double pitch, double speed, double rotSpeed, InputY teleport)
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
                GetMeshCollision(ref pos, collider, teleport);

            vp.SetCameraLocation(pos, true);
        }

        private void GetMeshCollision(ref Point3d pos, Mesh colMsh, InputY teleport)
        {
            Vector3d dir = (teleport == InputY.Up) ? Vector3d.ZAxis : -Vector3d.ZAxis;
            Ray3d ray = new(pos, dir);

            double distance = Intersection.MeshRay(colMsh, ray);

            if (teleport == InputY.Down || teleport == InputY.Up)
            {
                Teleport(ref pos, colMsh, teleport);
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

        private void Teleport(ref Point3d pos, Mesh colMsh, InputY teleport)
        {
            if (teleport == InputY.Default)
                return;

            RhinoApp.WriteLine($"JUmp: " + teleport);

            Point3d[] pts = Intersection.ProjectPointsToMeshes(new Mesh[] { colMsh }, new Point3d[] { pos }, Vector3d.ZAxis, 0.1);

            List<double> lst = new();

            for (int i = 0; i < pts.Length; i++)
            {
                Point3d pt = pts[i];
                double dist = pt.DistanceTo(pos);

                if (pt.Z > pos.Z)
                {
                    if (teleport == InputY.Up)
                        lst.Add(dist);
                }
                else if (teleport == InputY.Down)
                    lst.Add(dist);
            }

            lst.Sort();

            pos.Z += (teleport == InputY.Up) ? GetNextUp(lst, eyeHeight) : GetNextDown(lst, eyeHeight);
        }

        static double GetNextDown(List<double> lst, double eyeHeight)
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

        static double GetNextUp(List<double> lst, double eyeHeight)
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