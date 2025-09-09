using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Diagnostics;

namespace Daxs
{
    internal class HUD : DisplayConduit
    {
        private static readonly Lazy<HUD> _instance = new(() => new HUD());
        public static HUD Instance => _instance.Value;

        private readonly Stopwatch sw = new Stopwatch();
        private string text;
        private int durationMs;

        internal HUD() 
        {
            //Stopwatch.StartNew();
        }

        /// <summary>
        /// Updates stopwatch and deactivates HUD if expired.
        /// Called regularly from gamepad loop.
        /// </summary>
        public void Tick() 
        {
            if (Enabled && sw.ElapsedMilliseconds > durationMs)
            {
                text = "";
                Enabled = false;
            }
        }

        /// <summary>
        /// Sets the text and starts countdown.
        /// </summary>
        public void SetText(string text, int durationMs)
        {
            this.text = text;
            this.durationMs = durationMs;

            Enabled = true;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (e.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            e.Display.Draw2dText(text, System.Drawing.Color.DarkGreen, new Point2d(200, 200),true,20);
        }

        protected override void OnEnable(bool enable)
        {
            base.OnEnable(enable);

            if (enable) 
            {
                sw.Restart();
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
            else 
            {

                sw.Stop();
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }
    }
}
