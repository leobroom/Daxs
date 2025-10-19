using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    internal class LensAction : BaseState
    {
        private InputY mode;
        private double strength;
        private double actualLens =-1;
        private Settings settings;

        public LensAction( InputX Input, InputY mode) :base( Input)
        {
            settings = Settings.Instance;

            strength = settings.BindNumeric("LensStep", v => strength = v);

            actualLens = Math.Round(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);
        }

        public override string HUD_Name => $"Lens: " + actualLens;


        public override void Execute()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
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