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

        //Gamepad Layout
        private Dictionary<string, IGamepadLayout> layouts = new();
        private IGamepadLayout currentLayout;

        public event EventHandler<DisplayEventArgs> Message;
        private LayoutManager(){}

        public void RegisterLayout(IGamepadLayout layout) => layouts[layout.Name] = layout;

        public void SetLayout(string name)
        {
            if (layouts.TryGetValue(name, out var layout))
            {
                currentLayout = layout;
                Message?.Invoke(this, new DisplayEventArgs($"Layout: {name}"));
            }
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