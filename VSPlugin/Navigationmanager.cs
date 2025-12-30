using Rhino;
using Rhino.Geometry;
using System;

namespace Daxs
{
    internal sealed class NavigationManager
    {
        private static NavigationManager instance = null;
        public static NavigationManager Instance => instance ??= new NavigationManager();

        private NavigationMesh navMesh = null;

        public Guid NavMeshId=> (navMesh == null) ? Guid.Empty : navMesh.Id;

        public event EventHandler<Mesh> NavigationMeshChanged;

        public bool SetMeshById(Guid navMeshId)
        {
            Mesh mesh = FindMeshById(navMeshId);
            if (mesh == null)
            {
                Clear();

                NavigationMeshChanged?.Invoke(this, null);
                return false;
            }

            navMesh = new NavigationMesh(mesh, navMeshId);
            NavigationMeshChanged?.Invoke(this, mesh);


            return true;
        }

        private void Clear()
        {
            navMesh = null;
            NavigationMeshChanged?.Invoke(this, null);
        }

        public static Mesh FindMeshById(Guid id)
        {
            if(id == Guid.Empty)
                return null;

            var rhObj = RhinoDoc.ActiveDoc.Objects.Find(id);
            if (rhObj == null)
                return null;

            return rhObj.Geometry as Mesh;   // or DuplicateMesh()
        }
    }

    internal class NavigationMesh
    {
        public Guid Id { get; } = Guid.Empty;

        private readonly Mesh mesh = null;

        public NavigationMesh(Mesh mesh, Guid id)
        {
            this.mesh = mesh;
            this.Id = id;
        }
    }
}