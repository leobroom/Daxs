using Rhino;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using System;
using System.Linq;

namespace Daxs
{
    internal class NextViewport : BaseState
    {
        public NextViewport(InputX Input) : base(Input){}

        public override string HUD_Text => $"Switch to Viewport: {next.ActiveViewportID}";

        public override void Execute()
        {
            ViewTable viewTable = RhinoDoc.ActiveDoc?.Views;
            if (viewTable == null || next == null)
                return;

            RhinoView[] views = viewTable.ToList().ToArray();
            RhinoView active = viewTable.ActiveView;

            int index = Array.IndexOf(views, active);
            int prev = (index - 1 + views.Length) % views.Length;

            next = views[prev];
            _hud.SetText(HUD_Emoji, HUD_Text);

            viewTable.ActiveView = next;
            next.Redraw();
        }

        RhinoView next = null;
    }
}