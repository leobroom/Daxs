using Rhino;
using Rhino.Display;

namespace Daxs
{
    internal class LensAction : IAction
    {
        private LensInput mode;
        private readonly RhinoDoc doc = RhinoDoc.ActiveDoc;
        private readonly double defaultVal;
        private readonly double strength;

        public LensAction(LensInput mode, double strength)
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
                case LensInput.Up:
                    actual += strength;
                    break;
                case LensInput.Down:
                    actual -= strength;
                    break;
                case LensInput.Reset:
                    actual = defaultVal;
                    break;
            }

            vp.Camera35mmLensLength = actual;
        }
    }

    internal enum LensInput
    {
        Up,
        Down,
        Reset
    }
}