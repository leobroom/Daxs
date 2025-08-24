using Rhino;
using Rhino.Display;

namespace Daxs
{
    internal class LensAction : IAction
    {
        private InputVert mode;
        private readonly RhinoDoc doc = RhinoDoc.ActiveDoc;
        private readonly double defaultVal;
        private readonly double strength;

        public LensAction(InputVert mode, double strength)
        {
            this.mode = mode;
            this.strength = strength;
            defaultVal = doc.Views.ActiveView.ActiveViewport.Camera35mmLensLength;
        }

        public void Execute()
        {
            RhinoView view = doc.Views.ActiveView;
            RhinoViewport vp = view.ActiveViewport;
            double actual = vp.Camera35mmLensLength;

            switch (mode)
            {
                case InputVert.Up:
                    actual += strength;
                    break;
                case InputVert.Down:
                    actual -= strength;
                    break;
                case InputVert.Default:
                    actual = defaultVal;
                    break;
            }

            vp.Camera35mmLensLength = actual;
            view.Redraw();
        }
    }
}