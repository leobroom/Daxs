using Rhino;
using Rhino.Display;
using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class NextView : BaseState
    {
        public NextView(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Switched to {_nextName}";

        private readonly List<(DefinedViewportProjection proj, string name)> _planviews = new List<(DefinedViewportProjection, string)>()
        {
            (DefinedViewportProjection.Top, "Top"),
            (DefinedViewportProjection.Front, "Front"),
            (DefinedViewportProjection.Right, "Right"),
            (DefinedViewportProjection.Left, "Left"),
            (DefinedViewportProjection.Back, "Back"),
            (DefinedViewportProjection.Bottom, "Bottom")
        };

        private readonly List<(DefinedViewportProjection proj, string name)> _perspectiveviews =new List<(DefinedViewportProjection, string)>()
        {
            (DefinedViewportProjection.Perspective, "Perspective"),
            (DefinedViewportProjection.TwoPointPerspective, "Two Point Perspective")
        };

        private DefinedViewportProjection _nextProjection = DefinedViewportProjection.Perspective;
        private string _nextName = "unset";

        public override void Execute()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
                return;

            var vp = view.ActiveViewport;

            bool isPlan = vp.IsPlanView;
            string currentName = vp.Name;

            var list = isPlan ? _planviews : _perspectiveviews;

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
            (_nextProjection, _nextName) = list[next];

            _hud.SetText(HUD_Emoji, HUD_Text);

            vp.SetProjection(_nextProjection, _nextName, true);
            view.Redraw();
        }
    }
}