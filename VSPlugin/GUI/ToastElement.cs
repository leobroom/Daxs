using Rhino.Display;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Daxs.GUI
{
    internal sealed class ToastElement : IOverlayElement
    {
        public string Id => "toast";
        public bool Enabled { get; private set; }

        // Content
        private string _emoji = "🎮";
        private string _message = "";
        private int _durationMs;

        // Timing (single time base: HUD nowMs)
        private long _startMs;
        private bool _playInAnim;
        private long _endMs;        // start + duration (absolute end moment)


        // Optional: keep visible a hair longer so the last out frame renders
        private const int HideGraceMs = 34; // ~1 frame @30fps (or 17 for ~60fps)

        // Icon mode
        private Bitmap _iconGdi;
        private Size _iconPx = Size.Empty;

        // Animation
        private const int AnimInMs = 180;
        private const int AnimOutMs = 220;
        private const int SlidePx = 24;

        // Layout
        private static readonly Color ToastFg = Color.White;
        private const int ToastEmojiFontPx = 14;

        private const int ToastMarginPx = 18;
        private const int ToastInnerPadPx = 4;
        private const int ToastGapPx = 4;
        private const float ToastRadiusPx = 16f;

        // Cached rendered toast
        private Bitmap _cachedGdi;
        private DisplayBitmap _cachedDisplay;
        private int _cachedFontPx;
        private string _cachedKey;
        private int _stableWidth;

        public void SetText(string emoji, string message, int durationMs)
        {
            _emoji = string.IsNullOrWhiteSpace(emoji) ? "🎮" : emoji;
            _message = message ?? string.Empty;

            DisposeIcon();

            StartOrExtend(durationMs);
        }

        public void SetIcon(Bitmap icon, string message, int durationMs, int iconSizePx = 20)
        {
            _message = message ?? string.Empty;
            _emoji = null; // icon mode

            DisposeIcon();
            // If you can guarantee 'icon' lifetime, don't clone. Otherwise keep clone.
            _iconGdi = (Bitmap)icon.Clone();
            _iconPx = new Size(iconSizePx, iconSizePx);

            StartOrExtend(durationMs);
        }

        /// <summary>
        /// Starts or extends lifetime. Relies on Tick(nowMs) to provide current time base.
        /// If called while Enabled, extends from "now" not from original start.
        /// </summary>
        private void StartOrExtend(int durationMs)
        {
            durationMs = Math.Max(0, durationMs);
            _durationMs = durationMs;

            _cachedKey = null;

            if (!Enabled)
            {
                Enabled = true;
                _playInAnim = true;
                _stableWidth = 0;

                _startMs = -1;
                _endMs = -1;
            }
            else
            {
                // restart lifetime from "now" (we'll set start/end on next Tick)
                _playInAnim = false;

                _startMs = -1;
                _endMs = -1;
            }
        }

        public void Tick(long nowMs)
        {
            if (!Enabled)
                return;

            if (_startMs < 0)
                _startMs = nowMs;

            if (_durationMs > 0 && (nowMs - _startMs) >= _durationMs)
                Hide();
        }

        /// <summary>
        /// Expire element
        /// </summary>
        public void Hide()
        {
            _message = "";
            _emoji = "🎮";

            DisposeIcon();

            _stableWidth = 0;
            _playInAnim = false;
            _startMs = -1;


            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            _cachedKey = null;
            Enabled = false;
        }

        public void Draw(DisplayPipeline dp, RhinoViewport viewport, float uiScale, long nowMs)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(_message))
                return;

            long elapsed = (_startMs >= 0) ? (nowMs - _startMs) : 0;

            var vp = viewport.Size;

            float scale = uiScale;

            int fontPx = (int)(GUI_Utils.FONT_PX * scale);
            int margin = (int)(ToastMarginPx * scale);
            int innerPad = (int)(ToastInnerPadPx * scale);
            int gap = (int)(ToastGapPx * scale);
            float slidePx = SlidePx * scale;

            float slide = 0f;

            // Out animation starts at duration - AnimOutMs
            int outStart = Math.Max(0, _durationMs - AnimOutMs);

            if (_playInAnim && elapsed < AnimInMs)
            {
                float t = GUI_Utils.Clamp01(elapsed / (float)AnimInMs);
                slide = (1f - GUI_Utils.EaseOutCubic(t)) * slidePx;
            }
            else if (_durationMs > 0 && elapsed > outStart)
            {
                float t = GUI_Utils.Clamp01((elapsed - outStart) / (float)AnimOutMs);
                slide = GUI_Utils.EaseInCubic(t) * slidePx;
            }

            EnsureBitmap(fontPx, innerPad, gap, scale);

            if (_cachedGdi == null || _cachedDisplay == null)
                return;

            int x = vp.Width - margin - _cachedGdi.Width + (int)slide;
            int y = vp.Height - margin - _cachedGdi.Height;

            dp.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(int fontPx, int innerPad, int gap, float scale)
        {
            bool hasIcon = _iconGdi != null;

            string iconKey = hasIcon ? $"{_iconGdi.Width}x{_iconGdi.Height}@{_iconPx.Width}x{_iconPx.Height}" : "noicon";
            string key = $"{iconKey}|{_emoji}|{_message}|{fontPx}|{innerPad}|{gap}|{scale:0.###}";

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

            SizeF leftSize;
            SizeF msgSize;

            using (var tmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                msgSize = g.MeasureString(_message ?? string.Empty, font);

                if (hasIcon)
                {
                    int iconW = Math.Max(1, (int)MathF.Round(_iconPx.Width * scale));
                    int iconH = Math.Max(1, (int)MathF.Round(_iconPx.Height * scale));
                    leftSize = new SizeF(iconW, iconH);
                }
                else
                {
                    leftSize = g.MeasureString(_emoji ?? string.Empty, emojiFont);
                }
            }

            int w = (int)Math.Ceiling(innerPad + leftSize.Width + gap + msgSize.Width + innerPad);
            int h = (int)Math.Ceiling(innerPad + Math.Max(leftSize.Height, msgSize.Height) + innerPad);

            const int WidthChangeThresholdPx = 10;

            if (_stableWidth <= 0)
                _stableWidth = w;
            else
            {
                int diff = w - _stableWidth;
                if (Math.Abs(diff) >= WidthChangeThresholdPx)
                    _stableWidth = w;
            }

            w = _stableWidth;

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

                using var path = GUI_Utils.RoundedRect(rect, radius);
                using var bgBrush = new SolidBrush(GUI_Utils.BG_COLOR);
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
                    var eSize = g.MeasureString(_emoji ?? string.Empty, emojiFont);
                    var emojiPt = new PointF(x, centerY - eSize.Height / 2f);

                    g.DrawString(_emoji ?? string.Empty, emojiFont, fgBrush, emojiPt);
                    x += eSize.Width + gap;
                }

                var msgSize2 = g.MeasureString(_message ?? string.Empty, font);
                var msgPt = new PointF(x, centerY - msgSize2.Height / 2f);
                g.DrawString(_message ?? string.Empty, font, fgBrush, msgPt);
            }

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
        }

        private void DisposeIcon()
        {
            _iconGdi?.Dispose();
            _iconGdi = null;
            _iconPx = Size.Empty;
        }

        public void Dispose() => Hide();
    }
}