using Rhino;
using Rhino.Geometry;
using System;

namespace Daxs
{
    internal sealed class NavigationManager
    {
        private static NavigationManager instance;
        public static NavigationManager Instance => instance ??= new NavigationManager();

        private Guid _navMeshId = Guid.Empty;
        private Mesh _navMesh;

        public Guid NavMeshId => _navMeshId;

        // Always return a copy
        public Mesh NavMesh => _navMesh?.DuplicateMesh();

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

            _navMeshId = meshId;
            _navMesh = docMesh.DuplicateMesh();

            NavigationMeshChanged?.Invoke(this, _navMesh);
            return true;
        }

        /// <summary>
        /// Clears the Navigation Mesh
        /// </summary>
        public void Clear()
        {
            _navMeshId = Guid.Empty;
            _navMesh = null;
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