using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    internal class LensAction : BaseState
    {
        private InputY mode;
        private double strength, defaultLens, actualLens;
        private readonly Settings settings;

        public LensAction( InputX Input, InputY mode) :base( Input)
        {
            settings = Settings.Instance;
            this.mode = mode;

            strength = settings.BindNumeric("LensStep", v => strength = v);
            defaultLens = settings.BindNumeric("LensDefault", v => defaultLens = v);

            actualLens = Math.Round(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);
        }

        public override string HUD_Name => $"Lens: " + actualLens;


        public override void Execute()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            RhinoView view = doc.Views.ActiveView;
            RhinoViewport vp = view.ActiveViewport;
            actualLens = Math.Round(vp.Camera35mmLensLength);

            //RhinoApp.WriteLine("LensAction actualLens: " + actualLens.ToString() + " | strength : " + strength);

            switch (mode)
            {
                case InputY.Up:
                    actualLens += strength;
                    break;
                case InputY.Down:
                    actualLens -= strength;
                    break;
                case InputY.Default:
                    actualLens = defaultLens;
                    break;
            }

            //RhinoApp.WriteLine("LensAction actualLens: " + actualLens.ToString());

            Math.Round(actualLens);

            //RhinoApp.WriteLine("LensAction actualLens: " + actualLens.ToString());

            vp.Camera35mmLensLength = actualLens;
            view.Redraw();
        }
    }
}