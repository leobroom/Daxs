using System.Linq;
using System;

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.UI;

namespace Daxs.Commands
{
    public class SetNavigationMeshCmd : Command
    {
        public SetNavigationMeshCmd(){ Instance = this;}
        public static SetNavigationMeshCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_SetNavigationMesh";

        private const int _MAX_V_COUNT = 50000;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var preselection = doc.Objects.GetSelectedObjects(includeLights: false, includeGrips: false).Where(o => o?.Geometry is Mesh).ToList();

            Guid meshId = Guid.Empty;

            if (preselection.Count == 1)
            {
                meshId = preselection[0].Id;
                RhinoApp.WriteLine($"Preselected mesh used. ID = {meshId}");
            }
            else
            {
                if (preselection.Count > 1)
                    RhinoApp.WriteLine("Please select only ONE mesh. Multiple meshes are not allowed.");

                var go = new GetObject();
                go.SetCommandPrompt("Select ONE navigation mesh");
                go.GeometryFilter = ObjectType.Mesh;
                go.SubObjectSelect = false;

                // Forces exactly one selection
                go.GetMultiple(minimumNumber: 1, maximumNumber: 1);
                if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                var rhObj = go.Object(0)?.Object();
                if (rhObj == null)
                    return Result.Failure;

                meshId = rhObj.Id;
                RhinoApp.WriteLine($"Navigation mesh selected. ID = {meshId}");
            }

            Result isOk = IsMeshOK(doc, meshId);
            if (isOk != Result.Success)
                return isOk;

            NavigationManager.Instance.SetMeshById(meshId);
            return Result.Success;
        }

        /// <summary>
        /// Sanity Check of the mesh
        /// </summary>
        Result IsMeshOK(RhinoDoc doc , Guid meshId) 
        {
            var obj = doc.Objects.FindId(meshId);
            var mesh = obj?.Geometry as Mesh;
            if (mesh == null)
                return Result.Failure;

            if (mesh.Vertices.Count > _MAX_V_COUNT)
            {
                var msg =
                    $"The selected mesh has {mesh.Vertices.Count} vertices.\n\n" +
                    $"The recommended maximum is {_MAX_V_COUNT}.\n\n" +
                    "Large meshes may reduce navigation performance.\n\n" +
                    "Do you want to continue?";

                var result = Dialogs.ShowMessage(msg,"Large Navigation Mesh",ShowMessageButton.YesNo,ShowMessageIcon.Warning);
                if (result != ShowMessageResult.Yes)
                    return Result.Cancel;
            }

            return Result.Success;
        }
    }
}