using Rhino;
using Rhino.Display;
using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class NextView : BaseState, ICalculate
    {
        public NextView(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Switched to {nextName}";

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
            var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
            if (view == null) 
                return;

            var vp = view.ActiveViewport;

            vp.SetProjection(nextProjection, nextName, true);
            view.Redraw();
        }


        DefinedViewportProjection nextProjection = DefinedViewportProjection.Perspective;
        string nextName = "unset";

        public void Calculate()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null) 
                return;

            var vp = view.ActiveViewport;

            bool isPlan = vp.IsPlanView;
            string currentName = vp.Name;

            var list = isPlan ? planviews : perspectiveviews;

            int currentIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            int next = (currentIndex + 1) % list.Count;
            (nextProjection, nextName) = list[next];
        }
    }
}