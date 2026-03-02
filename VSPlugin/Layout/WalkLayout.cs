using System.Collections.Generic;
using Daxs.Settings;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Daxs.Layout
{
    internal class WalkLayout : FlyLayout
    {
        public override LayoutType Name => LayoutType.Walk;

        private Mesh navMesh = null;
        private double eyeHeight, maximalJump;

        public WalkLayout() : base()
        {
            var eH = (NumericValue)settings["EyeHeight"];
            var mj = (NumericValue)settings["MaximalJump"];

            eH.ValueChanged += (s, val) => eyeHeight = val;
            mj.ValueChanged += (s, val) => maximalJump = val;

            eyeHeight = eH.Value;
            maximalJump = mj.Value;
        }

        protected override Plane CalculateCamPlane(double cp, double cy, double sy, double sp, double forward, double strafe, double vertical, double speedMulti, double delta, InputY teleport)
        {
            var viewDir = new Vector3d(cp * cy, cp * sy, sp);
            var right = new Vector3d(-sy, cy, 0);
            var moveDir = new Vector3d(cy, sy, 0); //flatened XY

            Vector3d move = moveDir * (forward * speedMulti * delta)
                        + right * (strafe * speedMulti * delta)
                        + zAxis * (vertical * speedMulti * delta);

            Point3d pos = camPlane.Origin + move;

            //Collision
            if (navMesh != null)
                GetMeshCollision(ref pos, navMesh, teleport);

            return new Plane(pos, viewDir, right);
        }

        Point3d? lastPoint = null;

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
                RhinoApp.WriteLine("ClosestPoint");
                pos.Z -= eyeHeight;
                pos = colMsh.ClosestPoint(pos);
                pos.Z += eyeHeight;
            }

        }

        private readonly object navMeshLock = new object();

        public void SetNavigationMesh(Mesh newMesh)
        {
            Mesh safeCopy = newMesh?.DuplicateMesh();

            lock (navMeshLock)
            {
                navMesh = safeCopy;
            }
        }

        //----------Jump
        private void Teleport(ref Point3d pos, Mesh colMsh, InputY teleport)
        {
            if (teleport == InputY.Default)
                return;

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
            double addZ = (teleport == InputY.Up) ? GetNextUp(lst, eyeHeight) : GetNextDown(lst, eyeHeight);
            if (addZ != 0 && teleport!= InputY.Default)
                hud.SetText("🎮", "Teleport " + teleport.ToString());

            pos.Z += addZ;
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