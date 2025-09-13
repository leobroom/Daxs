using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.Drawing;

namespace Daxs
{
    internal class HUD : DisplayConduit
    {
        private static readonly Lazy<HUD> _instance = new(() => new HUD());
        public static HUD Instance => _instance.Value;

        private readonly Stopwatch sw = new();
        private string text;
        private int durationMs;

        // Tune these to taste
        private const double FontScale = 0.03;   // % of viewport height
        private const double MarginScale = 2.5;  // margin ≈ 0.6 * font size
        private const string FontFace = "Segoe UI"; // optional; omit if you want default

        internal HUD() { }

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

            sw.Restart();

            if (!Enabled)
                Enabled = true;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (e.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            var vp = e.Viewport.Size;
            int fontPx = (int)(vp.Height * FontScale);
            int margin = (int)(fontPx * MarginScale);

            Rectangle bounds = e.Display.Measure2dText(text, Point2d.Origin, true, 0, fontPx, FontFace);

            var pt = new Point2d(vp.Width - margin, vp.Height - margin);

            double x = vp.Width - margin - bounds.Width;
            double y = vp.Height - margin - bounds.Height;

            e.Display.Draw2dText(text,Color.Black,new Point2d(x, y),middleJustified: false,fontPx,FontFace);
        }

        protected override void OnEnable(bool enable)
        {
            base.OnEnable(enable);

            if (enable)
            {
                sw.Restart();
                RhinoDoc.ActiveDoc.Views.Redraw(); //HACK
            }
            else
            {
                sw.Stop();
                RhinoDoc.ActiveDoc.Views.Redraw(); //HACK
            }
        }
    }
}