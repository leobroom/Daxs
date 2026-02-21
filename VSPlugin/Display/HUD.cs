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
        private Bitmap _iconGdi;
        private Size _iconPx = Size.Empty;

        // Redraw throttling (avoid spamming redraw)
        private long lastRedrawMs;
        private const int RedrawEveryMs = 33;

        private bool textVisible = false;
        private double testTime = 2000;

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
            testTime = settings.BindNumeric("TextTime", t => testTime = t);

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
            emoji = "🎮";

            DisableIcon();

            _stableWidth = 0;
            _playInAnim = false;
            Enabled = false;
        }

        private void DisableIcon()
        {
            _iconGdi = null;
            _iconPx = Size.Empty;

        }

        /// <summary>
        /// Like SetText, but uses an icon bitmap instead of an emoji.
        /// The icon is cloned and owned by the HUD (safe if caller disposes theirs).
        /// </summary>
        public void SetImageToast(Bitmap icon, string message, int durationMs, int iconSizePx = 20)
        {
            if (!textVisible)
            {
                if (Enabled) DisableText();
                return;
            }

            this.message = message;

            this.emoji = null;
            _iconGdi = icon;
            _iconPx = new Size(iconSizePx, iconSizePx);

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

        public void SetText(string emoji, string message)
        {
            int durationMs = (int)Math.Round(testTime);
            SetText( emoji,  message,  durationMs);
        }

        /// <summary>
        /// Sets a toast payload and starts countdown.
        /// </summary>
        public void SetText(string emoji, string message, int durationMs)
        {
            if (!textVisible)
            {
                if (Enabled)
                    DisableText();
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
            if (!Enabled || string.IsNullOrWhiteSpace(message) || RhinoDoc.ActiveDoc == null || e.Viewport.Id != RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewportID)
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

            int x = vp.Width - margin - _cachedGdi.Width + (int)slide;
            int y = vp.Height - margin - _cachedGdi.Height;

            e.Display.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(int fontPx, int innerPad, int gap, float scale)
        {
            bool hasIcon = _iconGdi != null;

            string iconKey = hasIcon ? $"{_iconGdi.Width}x{_iconGdi.Height}@{_iconPx.Width}x{_iconPx.Height}" : "noicon";
            string key = $"{iconKey}|{emoji}|{message}|{fontPx}|{innerPad}|{gap}|{scale:0.###}";

            if (_cachedDisplay != null && _cachedFontPx == fontPx && _cachedKey == key)
                return;

            _cachedKey = key;
            _cachedFontPx = fontPx;

            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            // Fonts
            using var font = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel);
            using var emojiFont = new Font("Segoe UI Emoji", (int)(ToastEmojiFontPx * scale), FontStyle.Bold, GraphicsUnit.Pixel);

            // Measure
            SizeF leftSize;
            SizeF msgSize;

            using (var tmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                msgSize = g.MeasureString(message ?? string.Empty, font);

                if (hasIcon)
                {
                    int iconW = Math.Max(1, (int)MathF.Round(_iconPx.Width * scale));
                    int iconH = Math.Max(1, (int)MathF.Round(_iconPx.Height * scale));
                    leftSize = new SizeF(iconW, iconH);
                }
                else
                {
                    leftSize = g.MeasureString(emoji ?? string.Empty, emojiFont);
                }
            }

            int w = (int)Math.Ceiling(innerPad + leftSize.Width + gap + msgSize.Width + innerPad);
            int h = (int)Math.Ceiling(innerPad + Math.Max(leftSize.Height, msgSize.Height) + innerPad);

            const int WidthChangeThresholdPx = 10;

            if (Enabled)
            {
                if (_stableWidth <= 0)
                    _stableWidth = w;
                else
                {
                    int diff = w - _stableWidth;
                    if (Math.Abs(diff) >= WidthChangeThresholdPx)
                        _stableWidth = w;
                }

                w = _stableWidth;
            }
            else
                _stableWidth = w;

            w = Math.Max(1, w);
            h = Math.Max(1, h);

            _cachedGdi = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

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

                if (hasIcon)
                {
                    int iconW = Math.Max(1, (int)MathF.Round(_iconPx.Width * scale));
                    int iconH = Math.Max(1, (int)MathF.Round(_iconPx.Height * scale));

                    var dest = new Rectangle((int)MathF.Round(x), (int)MathF.Round(centerY - iconH / 2f), iconW, iconH);

                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(_iconGdi, dest);

                    x += iconW + gap;
                }
                else
                {
                    // Re-measure emoji to vertically center accurately
                    var eSize = g.MeasureString(emoji ?? string.Empty, emojiFont);
                    var emojiPt = new PointF(x, centerY - eSize.Height / 2f);

                    g.DrawString(emoji ?? string.Empty, emojiFont, fgBrush, emojiPt);

                    x += eSize.Width + gap;
                }

                // Message
                var msgPt = new PointF(x, centerY - msgSize.Height / 2f);
                g.DrawString(message ?? string.Empty, font, fgBrush, msgPt);
            }

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
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