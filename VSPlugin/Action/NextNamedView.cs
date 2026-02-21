using Rhino;
using Rhino.DocObjects;

namespace Daxs
{
    internal class NextNamedView : BaseState, ICalculate
    {
        public NextNamedView(InputX Input) : base(Input) { }

        public override string HUD_Text => $"Named view: {nextView?.Name}";

        public override void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc.Views.ActiveView;
            if (view == null || nextView == null)
                return;

            view.ActiveViewport.PushViewInfo(nextView, false);
            view.Redraw();
        }

        ViewInfo nextView = null;

        public void Calculate()
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

            nextView = namedViews[next];
        }
    }
}