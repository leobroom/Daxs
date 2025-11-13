using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input.Custom;
using Rhino.Geometry;
using System.Linq;

namespace Daxs
{
    public class SetNavigationMeshCmd : Command
    {
        public SetNavigationMeshCmd(){ Instance = this;}
        public static SetNavigationMeshCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_SetNavigationMesh";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh = null;

            // Try preselection
            var selected = doc.Objects.GetSelectedObjects(false, false).Where(o => o.Geometry is Mesh).ToList();

            if (selected.Count == 1)
            {
                mesh = (selected[0].Geometry as Mesh)?.DuplicateMesh();
                RhinoApp.WriteLine("Preselected mesh used.");
            }
            else
            {
                var gm = new GetObject();
                gm.SetCommandPrompt("Select a navigation mesh:");
                gm.GeometryFilter = ObjectType.Mesh;
                gm.DisablePreSelect();
                gm.SubObjectSelect = false;
                gm.Get();

                if (gm.CommandResult() != Result.Success)
                    return Result.Cancel;

                mesh = gm.Object(0).Mesh()?.DuplicateMesh();
                RhinoApp.WriteLine("Navigation Mesh was selected.");
            }

            //Set Mesh to
            LayoutManager.Instance.SetCollisionMesh(mesh);

            return Result.Success;
        }
    }
}