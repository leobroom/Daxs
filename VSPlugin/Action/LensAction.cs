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
        private double actualLens =-1;

        public LensAction(InputY mode, double strength)
        {
            this.mode = mode;
            this.strength = strength;
            actualLens = Math.Round(doc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);
        }

        public string HUD_Name => "Lens: " + actualLens;

        public void Execute()
        {
            RhinoView view = doc.Views.ActiveView;
            RhinoViewport vp = view.ActiveViewport;
            actualLens = Math.Round(vp.Camera35mmLensLength);

            switch (mode)
            {
                case InputY.Up:
                    actualLens += strength;
                    break;
                case InputY.Down:
                    actualLens -= strength;
                    break;
                case InputY.Default:
                    actualLens = strength;
                    break;
            }

            Math.Round(actualLens);

            vp.Camera35mmLensLength = actualLens;
            view.Redraw();
        }
    }
}