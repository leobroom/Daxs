using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input.Custom;
using Rhino.Geometry;
using System.Linq;
using System;

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
            Guid meshId = Guid.Empty;

            // Preselection
            var selected = doc.Objects.GetSelectedObjects(false, false)
                                      .Where(o => o.Geometry is Mesh)
                                      .ToList();

            if (selected.Count == 1)
            {
                var rhObj = selected[0];
                meshId = rhObj.Id;
                mesh = (rhObj.Geometry as Mesh)?.DuplicateMesh();

                RhinoApp.WriteLine($"Preselected mesh used. ID = {meshId}");
            }
            else
            {
                // User selection
                var gm = new GetObject();
                gm.SetCommandPrompt("Select a navigation mesh:");
                gm.GeometryFilter = ObjectType.Mesh;
                gm.DisablePreSelect();
                gm.SubObjectSelect = false;

                gm.Get();
                if (gm.CommandResult() != Result.Success)
                    return Result.Cancel;

                var rhObj = gm.Object(0).Object();
                meshId = rhObj.Id;
                mesh = gm.Object(0).Mesh()?.DuplicateMesh();

                RhinoApp.WriteLine($"Navigation mesh selected. ID = {meshId}");
            }

            // Store both mesh and ID
            LayoutManager.Instance.SetCollisionMesh(mesh, meshId);

            return Result.Success;
        }
    }
}