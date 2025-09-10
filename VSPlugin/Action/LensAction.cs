using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    internal class LensAction : IAction
    {
        private readonly InputY mode;
        private readonly RhinoDoc doc = RhinoDoc.ActiveDoc;
        private readonly double strength;

        public LensAction(InputY mode, double strength)
        {
            this.mode = mode;
            this.strength = strength;
        }

        public string HUD_Name => "Lens: " + Math.Round(doc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);

        public void Execute()
        {
            RhinoView view = doc.Views.ActiveView;
            RhinoViewport vp = view.ActiveViewport;
            double actual = vp.Camera35mmLensLength;

            switch (mode)
            {
                case InputY.Up:
                    actual += strength;
                    break;
                case InputY.Down:
                    actual -= strength;
                    break;
                case InputY.Default:
                    actual = strength;
                    break;
            }

            vp.Camera35mmLensLength = Math.Round(actual);
            view.Redraw();
        }
    }
}