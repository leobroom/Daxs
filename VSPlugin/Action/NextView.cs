using Rhino;
using Rhino.Display;
using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class NextView : BaseState
    {
        public NextView(InputX Input) : base(Input) { }

        public override string HUD_Name => $"Switch to next View";

        private readonly List<(DefinedViewportProjection proj, string name)> planviews = new List<(DefinedViewportProjection, string)>()
        {
            (DefinedViewportProjection.Top, "Top"),
            (DefinedViewportProjection.Front, "Front"),
            (DefinedViewportProjection.Right, "Right"),
            (DefinedViewportProjection.Left, "Left"),
            (DefinedViewportProjection.Back, "Back"),
            (DefinedViewportProjection.Bottom, "Bottom")
        };

        private readonly List<(DefinedViewportProjection proj, string name)> perspectiveviews =new List<(DefinedViewportProjection, string)>()
        {
            (DefinedViewportProjection.Perspective, "Perspective"),
            (DefinedViewportProjection.TwoPointPerspective, "Two Point Perspective")
        };

        public override void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc?.Views.ActiveView;
            if (view == null) return;

            var vp = view.ActiveViewport;

            bool isPlan = vp.IsPlanView;
            string currentName = vp.Name;

            var list = isPlan ? planviews : perspectiveviews;

            // find index by name
            int currentIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            // next projection
            int next = (currentIndex + 1) % list.Count;
            var (proj, name) = list[next];

            vp.SetProjection(proj, name, true);
            view.Redraw();
        }
    }
}