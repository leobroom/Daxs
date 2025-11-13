using Rhino;
using Rhino.Display;
using System;


namespace Daxs
{
    internal class NextDisplaymode : BaseState
    {
        DisplayModeDescription[] modes = new[]
        {
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.WireframeId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.RenderedId)
        };

        public NextDisplaymode(InputX Input) : base(Input) { }

        public override string HUD_Name => $"Switch to next Displaymode";

        public override void Execute()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
                return;

            var current = view.ActiveViewport.DisplayMode;
            int index = Array.FindIndex(modes, m => m.Id == current.Id);

            // If not in the first 3 display modes → restart at Wireframe (index 0)
            int next = (index == -1) ? 0 : (index + 1) % modes.Length;

            view.ActiveViewport.DisplayMode = modes[next];
            view.Redraw();

        }
    }
}