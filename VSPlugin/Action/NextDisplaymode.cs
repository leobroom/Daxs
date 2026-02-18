using Rhino;
using Rhino.Display;
using System;


namespace Daxs
{
    internal class NextDisplaymode : BaseState ,ICalculate
    {
        private readonly DisplayModeDescription[] modes = new[]
        {
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.WireframeId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.RenderedId)
        };

        public NextDisplaymode(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Displaymode: {nextDisplaymode.LocalName}";

        DisplayModeDescription nextDisplaymode = null;

        public override void Execute()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null || nextDisplaymode == null)
                return;

            view.ActiveViewport.DisplayMode = nextDisplaymode;
            view.Redraw();
        }

        public void Calculate()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
                return;

            var current = view.ActiveViewport.DisplayMode;
            int index = Array.FindIndex(modes, m => m.Id == current.Id);

            int next = (index == -1) ? 0 : (index + 1) % modes.Length;

            nextDisplaymode = modes[next];
        }
    }
}