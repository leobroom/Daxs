using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;

namespace Daxs
{
    internal class HUD : DisplayConduit, IDisposable
    {
        internal HUD() { }

        int count = 0;

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (e.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            e.Display.Draw2dText("Hello Daxs: " + count, System.Drawing.Color.DarkGreen, new Point2d(200, 200),true);
            count++;
        }

        public void Dispose()
        {
            Enabled = false;
            Dispose();
        }
    }
}
