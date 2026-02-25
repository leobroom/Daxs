using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    internal class LensAction : BaseState
    {
        private InputY _mode;
        private double strength, defaultLens, actualLens;
        private readonly Settings settings;

        public LensAction( InputX Input, InputY mode) :base( Input)
        {
            settings = Settings.Instance;
            this._mode = mode;

            strength = settings.BindNumeric("LensStep", v => strength = v);
            defaultLens = settings.BindNumeric("LensDefault", v => defaultLens = v);

           double? v= RhinoDoc.ActiveDoc?.Views?.ActiveView?.ActiveViewport.Camera35mmLensLength;

            if (v.HasValue)
                actualLens = Math.Round(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);
            else
                actualLens = 50;
        }

        public override string HUD_Text => $"Lens: " + actualLens;

        public override void Execute()
        {
            actualLens = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Camera35mmLensLength;

            switch (_mode)
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

            actualLens = Math.Round(actualLens);

            _hud.SetText(HUD_Emoji, HUD_Text);

            RhinoView view = RhinoDoc.ActiveDoc.Views.ActiveView;

            view.ActiveViewport.Camera35mmLensLength = actualLens;
            view.Redraw();
        }
    }
}