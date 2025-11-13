using Rhino;
using Rhino.DocObjects;

namespace Daxs
{
    internal class NextNamedView : BaseState
    {
        public NextNamedView(InputX Input) : base(Input) { }

        public override string HUD_Name => $"Switch to next Named View";

        public override void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
                return;

            var view = doc.Views.ActiveView;
            if (view == null)
                return;

            var namedViews = doc.NamedViews;
            int count = namedViews.Count;
            if (count == 0)
                return;

            var vp = view.ActiveViewport;

            // --- FIND CURRENT INDEX BY NAME ---
            string activeName = vp.Name;  // name of active viewport
            int index = -1;

            for (int i = 0; i < count; i++)
            {
                if (namedViews[i].Name == activeName)
                {
                    index = i;
                    break;
                }
            }

            // If no match -> start at first named view
            int next = (index == -1) ? 0 : (index + 1) % count;

            // Apply the named view
            ViewInfo target = namedViews[next];
            vp.PushViewInfo(target, false);

            view.Redraw();
        }
    }
}