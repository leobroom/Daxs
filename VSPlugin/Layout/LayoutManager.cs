using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Daxs
{
    public sealed class LayoutManager
    {
        private static readonly Lazy<LayoutManager> _instance = new(() => new LayoutManager());
        public static LayoutManager Instance => _instance.Value;

        public IGamepadLayout CurrentLayout => currentLayout;
        private IGamepadLayout currentLayout;

        public IGamepadLayout PreviousLayout => previousLayout;
        private IGamepadLayout previousLayout;

        private readonly Dictionary<string, IGamepadLayout> layouts = new();
        
        public event EventHandler<DisplayEventArgs> Message;
        private LayoutManager()
        {
            Register(new FlyLayout());
            Register(new WalkLayout());
            Register(new MenuLayout());
        }

        private void Register(IGamepadLayout layout) => layouts[layout.Name] = layout;

        public void Set(string name)
        {
            if (layouts.TryGetValue(name, out var layout))
            {
                previousLayout = (previousLayout == null) ? layout : currentLayout;
                RhinoApp.WriteLine("SetLayout:" + name);
                currentLayout = layout;
                Message?.Invoke(this, new DisplayEventArgs($"Layout: {name}"));
            }
        }

        public void SetToPreviousLayout() 
        {
            RhinoApp.WriteLine("SetToPreviousLayout");
            string mode = (previousLayout == null || currentLayout == null) ? "Fly" : previousLayout.Name;
            Set(mode);
        }

        public IGamepadLayout GetLayout(string name)
        {
            if (!layouts.TryGetValue(name, out var layout))
                throw new KeyNotFoundException($"Layout '{name}' not found.");
            return layout;
        }

        public void SetCollisionMesh(Mesh colMesh)
        {
            WalkLayout wLayout = (WalkLayout)GetLayout("Walk");
            wLayout.SetCollider(colMesh);
        }
    }
}