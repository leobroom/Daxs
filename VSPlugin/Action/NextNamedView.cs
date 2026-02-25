using Rhino;
using Rhino.DocObjects;

namespace Daxs
{
    internal class NextNamedView : BaseState
    {
        public NextNamedView(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Named view: {_nextView?.Name}";


        private ViewInfo _nextView = null;

        public override void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc.Views.ActiveView;
            if (view == null)
                return;

            var namedViews = doc.NamedViews;
            int count = namedViews.Count;
            if (count == 0)
                return;

            string activeName = view.ActiveViewport.Name;
            int index = -1;

            for (int i = 0; i < count; i++)
            {
                if (namedViews[i].Name == activeName)
                {
                    index = i;
                    break;
                }
            }
            int next = (index == -1) ? 0 : (index + 1) % count;

            _nextView = namedViews[next];
            if ( _nextView == null)
                return;

            _hud.SetText(HUD_Emoji, HUD_Text);

            view.ActiveViewport.PushViewInfo(_nextView, false);
            view.Redraw();
        }
    }
}