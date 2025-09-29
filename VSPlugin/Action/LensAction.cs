using Rhino;
using Rhino.Display;
using System;

namespace Daxs
{
    internal class LensAction : BaseState, IAction
    {
        private InputY mode;
        private readonly double strength;
        private double actualLens =-1;

        public LensAction( GButton Button, InputX Input, InputY mode, double strength) :base(AProperty.Lens,  Button,  Input)
        {

            this.strength = strength;
            actualLens = Math.Round(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.Camera35mmLensLength);
        }

        public LensAction(GButton Button, InputX Input, object[] args) : this( Button,  Input, (InputY)args[0],(double)args[1]){}

        public string HUD_Name => $"Lens: " + actualLens;

        public void Execute()
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


        //https://chatgpt.com/g/g-p-67e9bd1beeac8191a0f9ff9d384c27a1-xboxcontroller/c/68d57c92-667c-8329-bbbc-d3d9893e2b25

        public override object[] GetArgs()
        {
            return new object[] { mode, strength };
        }
    }
}