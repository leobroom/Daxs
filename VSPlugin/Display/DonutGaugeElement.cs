using Rhino.Display;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Daxs
{
    internal sealed class DonutGaugeElement : IOverlayElement
    {
        public string Id => "donut";
        public bool Enabled { get; private set; }

        private string _title = "Value";
        private double _value = 0.0;        // 0..10

        // Lifetime (HUD timebase)
        private long _startMs = -1;
        private long _endMs = 0;            // 0 => infinite

        // Layout
        private const int BaseSizePx = 80;
        private const float RingThicknessPx = 4f;
        private const float PaddingPx = 8f;
        private const int FONT_TITLEPX = 16;

        private float _startDeg = 20f;
        private float _endDeg = 340f;

        // Cache
        private Bitmap _cachedGdi;
        private DisplayBitmap _cachedDisplay;
        private string _cachedKey;

        public void Set(string title, double value0to10, double startDeg, double endDeg, int durationMs = 0)
        {
            _title = title ?? "Value";
            _value = Math.Clamp(value0to10, 0.0, 10.0);
            _startDeg = (float)startDeg;
            _endDeg = (float)endDeg;

            durationMs = Math.Max(0, durationMs);

            // Invalidate cache on any new Set
            _cachedKey = null;

            if (!Enabled)
            {
                Enabled = true;
                _startMs = -1; // will be initialized on next Tick(nowMs)
                _endMs = (durationMs > 0) ? -1 : 0; // -1 means "compute when startMs known"
            }
            else
            {
                // Extend/restart expiry predictably from "now"
                // We don't know nowMs here (API), so we set flags and handle in Tick(nowMs).
                if (durationMs > 0)
                {
                    // mark as "restart from next tick"
                    _startMs = -1;
                    _endMs = -durationMs; // encode requested duration
                }
                else
                {
                    _endMs = 0; // infinite
                }
            }
        }

        public void Hide()
        {
            _cachedDisplay?.Dispose(); _cachedDisplay = null;
            _cachedGdi?.Dispose(); _cachedGdi = null;
            _cachedKey = null;

            _startMs = -1;
            _endMs = 0;

            Enabled = false;
        }

        public void Tick(long nowMs)
        {
            if (!Enabled)
                return;

            // Initialize / restart lifetime when requested
            if (_startMs < 0)
            {
                _startMs = nowMs;

                if (_endMs == -1)
                {
                    // first enable with finite duration was requested, but duration value isn't stored in this path
                    // so treat as infinite unless you prefer otherwise
                    _endMs = 0;
                }
                else if (_endMs < 0)
                {
                    // encoded durationMs in negative
                    int durationMs = (int)Math.Min(int.MaxValue, -_endMs);
                    _endMs = _startMs + durationMs;
                }
            }

            if (_endMs > 0 && nowMs >= _endMs)
                Hide();
        }

        public void Draw(DisplayPipeline dp, RhinoViewport viewport, float uiScale, long nowMs)
        {
            if (!Enabled)
                return;

            EnsureBitmap(uiScale);

            if (_cachedDisplay == null || _cachedGdi == null)
                return;

            var vp = viewport.Size;

            int x = (vp.Width - _cachedGdi.Width) / 2;
            float bottomPadding = 18f * uiScale;

            int y = vp.Height - _cachedGdi.Height - (int)MathF.Round(bottomPadding);

            dp.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(float uiScale)
        {
            int size = Math.Max(1, (int)MathF.Round(BaseSizePx * uiScale));
            float ring = RingThicknessPx * uiScale;
            float pad = PaddingPx * uiScale;

            // quantize to reduce rebuilds
            double q = Math.Round(_value * 10.0) / 10.0; // 0.1 steps

            string key = $"{size}|{uiScale:0.###}|{_title}|{q:0.0}|{_startDeg:0.###}|{_endDeg:0.###}";
            if (_cachedKey == key && _cachedDisplay != null)
                return;

            _cachedKey = key;

            _cachedDisplay?.Dispose(); _cachedDisplay = null;
            _cachedGdi?.Dispose(); _cachedGdi = null;

            _cachedGdi = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using var g = Graphics.FromImage(_cachedGdi);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.Transparent);

            using (var bg = new SolidBrush(UIUtils.BG_COLOR))
                g.FillEllipse(bg, 0, 0, size - 1, size - 1);

            float t = (float)(_value / 10.0); // 0..1
            float startDeg = 90f + _startDeg;
            float sweepDeg = SweepDegrees(_startDeg, _endDeg);

            var ringRect = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad);

            using (var penTrack = new Pen(Color.FromArgb(70, 255, 255, 255), ring))
            {
                penTrack.StartCap = LineCap.Round;
                penTrack.EndCap = LineCap.Round;
                g.DrawArc(penTrack, ringRect, startDeg, sweepDeg);
            }

            using (var penFg = new Pen(Color.White, ring))
            {
                penFg.StartCap = LineCap.Round;
                penFg.EndCap = LineCap.Round;

                float valueSweep = sweepDeg * t;
                g.DrawArc(penFg, ringRect, startDeg, valueSweep);
            }

            string valueText = q.ToString("0.0");
            using var fontValue = new Font("Segoe UI", (int)(FONT_TITLEPX * uiScale), FontStyle.Bold, GraphicsUnit.Pixel);
            using var fontTitle = new Font("Segoe UI", (int)(UIUtils.FONT_PX * uiScale), FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.White);

            float centerX = size / 2f;
            float centerY = size / 2f;

            string titleText = _title ?? "";

            using var format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisWord
            };
            format.FormatFlags &= ~StringFormatFlags.NoWrap;

            float titleMaxHeight = (float)Math.Ceiling(fontTitle.GetHeight(g) * 2.2f);
            SizeF titleMeasured = g.MeasureString(titleText, fontTitle, size, format);
            float titleH = Math.Min(titleMeasured.Height, titleMaxHeight);

            var titleRect = new RectangleF(0, centerY - titleH / 2f, size, titleH);
            g.DrawString(titleText, fontTitle, brush, titleRect, format);

            var valueSize = g.MeasureString(valueText, fontValue);

            float bottomMargin = 4f * uiScale;
            g.DrawString(valueText, fontValue, brush,
                centerX - valueSize.Width / 2f,
                size - valueSize.Height - bottomMargin);

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
        }

        private static float SweepDegrees(float startDeg, float endDeg)
        {
            startDeg = Mod360(startDeg);
            endDeg = Mod360(endDeg);

            float sweep = endDeg - startDeg;
            if (sweep < 0) sweep += 360f;
            return sweep;
        }

        private static float Mod360(float deg)
        {
            deg %= 360f;
            if (deg < 0) deg += 360f;
            return deg;
        }

        public void Dispose() => Hide();
    }
}