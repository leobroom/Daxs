using Rhino;
using Rhino.Commands;
using System;

namespace Daxs.Commands
{
    public class ClearNavigationMeshCmd : Command
    {
        public ClearNavigationMeshCmd() { Instance = this; }
        public static ClearNavigationMeshCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_ClearNavigationMesh";

        NavigationManager _navManager = NavigationManager.Instance; 

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string msg = "Navigation mesh does not exists!";

            if (_navManager.NavMesh != null|| _navManager.NavMeshId == Guid.Empty)
            {
                msg = $"Navigation mesh with id: {_navManager.NavMeshId} cleared ";
                _navManager.Clear();
            }

            RhinoApp.WriteLine(msg);

            return Result.Success;
        }
    }
}
