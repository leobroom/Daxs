using Rhino;
using Rhino.Display;
using System;


namespace Daxs
{
    internal class NextDisplaymode : BaseState
    {
        private readonly DisplayModeDescription[] _modes = new[]
        {
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.WireframeId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId),
            DisplayModeDescription.GetDisplayMode(DisplayModeDescription.RenderedId)
        };

        public NextDisplaymode(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Displaymode: {_nextDisplaymode.LocalName}";

        private DisplayModeDescription _nextDisplaymode = null;

        public override void Execute()
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (view == null)
                return;

            var current = view.ActiveViewport.DisplayMode;
            int index = Array.FindIndex(_modes, m => m.Id == current.Id);

            int next = (index == -1) ? 0 : (index + 1) % _modes.Length;

            _nextDisplaymode = _modes[next];

            _hud.SetText(HUD_Emoji, HUD_Text);

            if ( _nextDisplaymode == null)
                return;

            view.ActiveViewport.DisplayMode = _nextDisplaymode;
            view.Redraw();
        }
    }
}