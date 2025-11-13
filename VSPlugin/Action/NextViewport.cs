using Rhino;
using System;
using System.Linq;

namespace Daxs
{
    internal class NextViewport : BaseState
    {
        public NextViewport(InputX Input) : base(Input){}

        public override string HUD_Name => $"Switch to next Viewport";

        public override void Execute()
        {
            var views = RhinoDoc.ActiveDoc.Views.ToList().ToArray();
            var active = RhinoDoc.ActiveDoc.Views.ActiveView;

            int index = Array.IndexOf(views, active);
            int prev = (index - 1 + views.Length) % views.Length;

            RhinoDoc.ActiveDoc.Views.ActiveView = views[prev];
            views[prev].Redraw();
        }
    }
}