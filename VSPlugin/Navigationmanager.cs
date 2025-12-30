using Rhino;
using Rhino.Geometry;
using System;

namespace Daxs
{
    internal sealed class NavigationManager
    {
        private static NavigationManager instance;
        public static NavigationManager Instance => instance ??= new NavigationManager();

        private Guid navMeshId = Guid.Empty;
        private Mesh navMesh;

        public Guid NavMeshId => navMeshId;

        // Always return a copy
        public Mesh NavMesh => navMesh?.DuplicateMesh();

        public event EventHandler<Mesh> NavigationMeshChanged;

        public bool SetMeshById(Guid meshId)
        {
            if (meshId == Guid.Empty)
            {
                Clear();
                return false;
            }

            var docMesh = FindMeshById(meshId);
            if (docMesh == null)
            {
                Clear();
                return false;
            }

            navMeshId = meshId;
            navMesh = docMesh.DuplicateMesh();

            NavigationMeshChanged?.Invoke(this, navMesh);
            return true;
        }

        private void Clear()
        {
            navMeshId = Guid.Empty;
            navMesh = null;
            NavigationMeshChanged?.Invoke(this, null);
        }

        private static Mesh FindMeshById(Guid id)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
                return null;

            var rhObj = doc.Objects.Find(id);
            return rhObj?.Geometry as Mesh;
        }
    }
}
