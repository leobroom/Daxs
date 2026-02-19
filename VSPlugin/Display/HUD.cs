using Rhino;
using Rhino.Display;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Daxs
{
    internal class HUD : DisplayConduit
    {
        private static readonly Lazy<HUD> _instance = new(() => new HUD());
        public static HUD Instance => _instance.Value;

        private readonly Settings settings = Settings.Instance;

        private readonly Stopwatch sw = new();

        // Toast content
        private string emoji = "🎮";
        private string message = "";
        private int durationMs;

        // Animation
        private const int AnimInMs = 180;
        private const int AnimOutMs = 220;
        private const int SlidePx = 24;

        // Cached rendered toast
        private Bitmap _cachedGdi;
        private DisplayBitmap _cachedDisplay;
        private int _cachedFontPx;
        private string _cachedKey;
        private bool _playInAnim;
        private int _stableWidth;
        private bool _idleHooked;

        // Redraw throttling (avoid spamming redraw)
        private long lastRedrawMs;
        private const int RedrawEveryMs = 33;

        private bool textVisible = false;

        //LAYOUT
        private static readonly Color ToastBg = Color.FromArgb(200, 0, 0, 0);
        private static readonly Color ToastFg = Color.White;
        private const int ToastFontPx = 12;        // text font size
        private const int ToastEmojiFontPx = 14;   // emoji font size
        private const int ToastMarginPx = 18;      // distance from bottom/right
        private const int ToastInnerPadPx = 4;    // pill padding
        private const int ToastGapPx = 4;         // gap emoji->text
        private const float ToastRadiusPx = 16f;   // rounded corner radius

        internal HUD()
        {
            textVisible = settings.BindBoolean("TextVisible", t => textVisible = t);

            // Ensure Tick() runs even when gamepad loop isn't active
            if (!_idleHooked)
            {
                _idleHooked = true;
                RhinoApp.Idle += (_, __) => Tick();
            }
        }

        /// <summary>
        /// Updates stopwatch and deactivates HUD if expired.
        /// Called regularly from gamepad loop.
        /// </summary>
        public void Tick()
        {
            if (!Enabled)
                return;

            long elapsed = sw.ElapsedMilliseconds;
            if (elapsed > durationMs)
            {
                DisableText();
                return;
            }

            // Drive animation.
            if (elapsed - lastRedrawMs >= RedrawEveryMs)
            {
                lastRedrawMs = elapsed;
                RhinoDoc.ActiveDoc?.Views?.Redraw();
            }
        }

        private void DisableText()
        {
            message = "";
            _stableWidth = 0;
            _playInAnim = false;
            Enabled = false;
        }

        /// <summary>
        /// Sets a toast payload and starts countdown.
        /// </summary>
        public void SetText(string emoji, string message, int durationMs)
        {
            if (!textVisible)
            {
                if (Enabled) DisableText();
                return;
            }

            this.emoji = emoji;
            this.message = message;

            if (Enabled)
            {
                long elapsed = sw.ElapsedMilliseconds;
                this.durationMs = (int)Math.Min(int.MaxValue, elapsed + durationMs);

                _playInAnim = false;
                _cachedKey = null;
                lastRedrawMs = 0;
                RhinoDoc.ActiveDoc?.Views?.Redraw();
                return;
            }

            this.durationMs = durationMs;
            _playInAnim = true;
            _stableWidth = 0;

            sw.Restart();
            lastRedrawMs = 0;
            Enabled = true;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(message) ||RhinoDoc.ActiveDoc == null || e.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
                return;

            var vp = e.Viewport.Size;

            float scale = UIUtils.GetWindowsScale();

            int fontPx = (int)(ToastFontPx * scale);
            int margin = (int)(ToastMarginPx * scale);
            int innerPad = (int)(ToastInnerPadPx * scale);
            int gap = (int)(ToastGapPx * scale);
            float slidePx = SlidePx * scale;

            // Slide animation
            long elapsed = sw.ElapsedMilliseconds;
            float slide = 0f;

            int outStart = Math.Max(0, durationMs - AnimOutMs);

            if (_playInAnim && elapsed < AnimInMs)
            {
                float t = UIUtils.Clamp01(elapsed / (float)AnimInMs);
                slide = (1f - UIUtils.EaseOutCubic(t)) * slidePx;
            }
            else if (elapsed > outStart)
            {
                float t = UIUtils.Clamp01((elapsed - outStart) / (float)AnimOutMs);
                slide = UIUtils.EaseInCubic(t) * slidePx;
            }

            EnsureBitmap(fontPx, innerPad, gap, scale);

            if (_cachedGdi == null || _cachedDisplay == null)
                return;

            int w = _cachedGdi.Width;
            int h = _cachedGdi.Height;

            int x = vp.Width - margin - w + (int)slide;
            int y = vp.Height - margin - h;

            e.Display.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(int fontPx, int innerPad, int gap, float scale)
        {
            string key = $"{emoji}|{message}|{fontPx}|{innerPad}|{gap}|{scale:0.###}";

            if (_cachedDisplay != null && _cachedFontPx == fontPx && _cachedKey == key)
                return;

            _cachedKey = key;
            _cachedFontPx = fontPx;

            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            using var font = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel);
            using var emojiFont = new Font("Segoe UI Emoji", (int)(ToastEmojiFontPx * scale), FontStyle.Bold, GraphicsUnit.Pixel);
            
            SizeF emojiSize, msgSize;
            using (var tmp = new System.Drawing.Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                emojiSize = g.MeasureString(emoji, emojiFont);
                msgSize = g.MeasureString(message, font);
            }

            int w = (int)Math.Ceiling(innerPad + emojiSize.Width + gap + msgSize.Width + innerPad);
            int h = (int)Math.Ceiling(innerPad + Math.Max(emojiSize.Height, msgSize.Height) + innerPad);

            // Keep width stable while visible to avoid "jumping" when text changes shorter
            if (Enabled)
            {
                if (_stableWidth <= 0) _stableWidth = w;
                _stableWidth = Math.Max(_stableWidth, w);
                w = _stableWidth;
            }

            _cachedGdi = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using (var g = Graphics.FromImage(_cachedGdi))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);

                var rect = new RectangleF(0, 0, w, h);
                float radius = ToastRadiusPx * scale;

                using var path = UIUtils.RoundedRect(rect, radius);
                using var bgBrush = new SolidBrush(ToastBg);
                using var fgBrush = new SolidBrush(ToastFg);

                g.FillPath(bgBrush, path);

                float x = innerPad;
                float centerY = h / 2f;

                // Emoji
                var emojiPt = new PointF(x, centerY - emojiSize.Height / 2f);
                g.DrawString(emoji, emojiFont, fgBrush, emojiPt);
                x += emojiSize.Width + gap;

                // Message
                var msgPt = new PointF(x, centerY - msgSize.Height / 2f);
                g.DrawString(message, font, fgBrush, msgPt);
            }

            _cachedDisplay = new Rhino.Display.DisplayBitmap(_cachedGdi);
        }

        protected override void OnEnable(bool enable)
        {
            base.OnEnable(enable);

            if (enable)
                sw.Restart();
            else
            {
                sw.Stop();

                _cachedDisplay?.Dispose();
                _cachedDisplay = null;

                _cachedGdi?.Dispose();
                _cachedGdi = null;
            }

            RhinoDoc.ActiveDoc?.Views?.Redraw();
        }
    }
}