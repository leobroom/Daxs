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

        //Gamepad Layout
        private Dictionary<string, IGamepadLayout> layouts = new();
        

        public event EventHandler<DisplayEventArgs> Message;
        private LayoutManager(){}

        public void RegisterLayout(IGamepadLayout layout) => layouts[layout.Name] = layout;

        public void SetLayout(string name)
        {
            if (layouts.TryGetValue(name, out var layout))
            {
                previousLayout = (previousLayout == null) ? layout : currentLayout;

                currentLayout = layout;
                Message?.Invoke(this, new DisplayEventArgs($"Layout: {name}"));
            }
        }

        public void SetToPreviousLayout() 
        {

            string mode = (previousLayout == null || currentLayout == null) ? "Fly" : previousLayout.Name;
            SetLayout(mode);
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