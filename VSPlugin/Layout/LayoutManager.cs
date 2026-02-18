using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Daxs
{
    public enum Layout
    {
        Fly,
        Walk,
        Menu
    }

    public sealed class LayoutManager
    {
        private static readonly Lazy<LayoutManager> _instance = new(() => new LayoutManager());
        public static LayoutManager Instance => _instance.Value;

        public IGamepadLayout Current => currentLayout;
        private IGamepadLayout currentLayout;

        public IGamepadLayout PreviousLayout => previousLayout;
        private IGamepadLayout previousLayout;

        private readonly Dictionary<Layout, IGamepadLayout> layouts = new();
        
        private LayoutManager()
        {
            Register(new FlyLayout());
            Register(new WalkLayout());
            Register(new MenuLayout());

            //Navigation
            NavigationManager navMan = NavigationManager.Instance;
            SetCollisionMesh(navMan.NavMesh);

            navMan.NavigationMeshChanged += (s, mesh) => SetCollisionMesh(mesh);
        }

        private void Register(IGamepadLayout layout) => layouts[layout.Name] = layout;

        public void Set(Layout name)
        {
            if (layouts.TryGetValue(name, out var layout))
            {
                previousLayout = (previousLayout == null) ? layout : currentLayout;
                //RhinoApp.WriteLine("SetLayout:" + name);
                currentLayout = layout;
            }
        }

        public void SetToPreviousLayout() 
        {   
            Layout mode = (previousLayout == null || currentLayout == null) ? Layout .Fly: previousLayout.Name;

            RhinoApp.WriteLine("SetToPreviousLayout: " + mode);
            Set(mode);
        }

        public IGamepadLayout Get(Layout name)
        {
            if (!layouts.TryGetValue(name, out var layout))
                throw new KeyNotFoundException($"Layout '{name}' not found.");
            return layout;
        }

        private void SetCollisionMesh(Mesh colMesh)
        {
            WalkLayout wLayout = (WalkLayout)Get(Layout.Walk);
            wLayout.SetNavigationMesh(colMesh);
        }
    }
}