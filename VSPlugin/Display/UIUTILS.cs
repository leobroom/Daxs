using Rhino;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Daxs
{
    internal static class UIUtils
    {
        internal static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();

            if (d > bounds.Width)
                d = bounds.Width;
            if (d > bounds.Height)
                d = bounds.Height;

            var arc = new RectangleF(bounds.X, bounds.Y, d, d);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        internal static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        internal static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }

        internal static float EaseInCubic(float t) => t * t * t;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        internal static float GetWindowsScale()
        {
            try
            {
                var hwnd = RhinoApp.MainWindowHandle();
                if (hwnd == IntPtr.Zero)
                    return 1f;
                return GetDpiForWindow(hwnd) / 96f;
            }
            catch
            {
                return 1f;
            }
        }
    }
}