using Rhino.Display;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Daxs
{
    internal sealed class DonutGaugeElement : IOverlayElement
    {
        public string Id => "donut";
        public bool Enabled { get; private set; }

        private readonly Settings settings =Settings.Instance;
        private readonly Stopwatch sw = new();

        private string _title = "Value";
        private double _value = 0.0;        // 0..10
        private int _durationMs = 0;         // 0 => infinite

        // Layout
        private const int BaseSizePx = 80;          // diameter before scale
        private const float RingThicknessPx = 4f;   // before scale
        private const float PaddingPx = 8f;         // before scale
        private const int FONT_TITLEPX = 16;

        private float _startDeg = 20f;
        private float _endDeg = 340f;

        // Cache
        private Bitmap _cachedGdi;
        private DisplayBitmap _cachedDisplay;
        private string _cachedKey;

        public DonutGaugeElement() { }

        public void Set(string title, double value0to10, double startDeg, double endDeg,  int durationMs = 0)
        {
            _title = title ?? "Value";
            _value = Math.Clamp(value0to10, 0.0, 10.0);
            _durationMs = Math.Max(0, durationMs);
            _startDeg = (float)startDeg;
            _endDeg = (float)endDeg;

            if (!Enabled)
            {
                sw.Restart();
                Enabled = true;
            }
            else
            {
                // refresh cache
                _cachedKey = null;
                if (_durationMs > 0)
                {
                    long elapsed = sw.ElapsedMilliseconds;
                    _durationMs = (int)Math.Min(int.MaxValue, elapsed + _durationMs);
                }
            }
        }

        public void Hide()
        {
            _cachedDisplay?.Dispose(); _cachedDisplay = null;
            _cachedGdi?.Dispose(); _cachedGdi = null;
            _cachedKey = null;
            Enabled = false;
        }

        public void Tick(long nowMs)
        {
            if (!Enabled) 
                return;

            if (_durationMs > 0 && sw.ElapsedMilliseconds > _durationMs)
                Hide();
        }

        public void Draw(DisplayPipeline dp, RhinoViewport viewport, float uiScale)
        {
            if (!Enabled)
                return;

            EnsureBitmap(uiScale);

            if (_cachedDisplay == null || _cachedGdi == null)
                return;

            var vp = viewport.Size;

            int x = (vp.Width - _cachedGdi.Width) / 2;

            float bottomPadding = 18f * uiScale;

            int y = vp.Height
                    - _cachedGdi.Height
                    - (int)MathF.Round(bottomPadding);

            dp.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(float uiScale)
        {
            int size = Math.Max(1, (int)MathF.Round(BaseSizePx * uiScale));
            float ring = RingThicknessPx * uiScale;
            float pad = PaddingPx * uiScale;

            // cache key
            string key = $"{size}|{uiScale:0.###}|{_title}|{_value:0.###}";
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

            // Background circle
            using (var bg = new SolidBrush(UIUtils.BG_COLOR))
            {
                g.FillEllipse(bg, 0, 0, size - 1, size - 1);
            }

            // Donut ring (sweep)
            float t = (float)(_value / 10.0); // 0..1

            float startDeg = 90f + _startDeg;
            float sweepDeg = SweepDegrees(_startDeg, _endDeg); // always positive, 0..360

            var ringRect = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad);

            // Optional track (faint full sweep)
            using (var penTrack = new Pen(Color.FromArgb(70, 255, 255, 255), ring))
            {
                penTrack.StartCap = LineCap.Round;
                penTrack.EndCap = LineCap.Round;
                g.DrawArc(penTrack, ringRect, startDeg, sweepDeg);
            }

            // Value arc
            using (var penFg = new Pen(Color.White, ring))
            {
                penFg.StartCap = LineCap.Round;
                penFg.EndCap = LineCap.Round;

                float valueSweep = sweepDeg * t;
                g.DrawArc(penFg, ringRect, startDeg, valueSweep);
            }

            // Center text
            string valueText = _value.ToString("0.0");
            using var fontValue = new Font("Segoe UI", (int)FONT_TITLEPX * uiScale, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fontTitle = new Font("Segoe UI", (int)UIUtils.FONT_PX * uiScale, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.White);

            var valueSize = g.MeasureString(valueText, fontValue);
            var titleSize = g.MeasureString(_title, fontTitle);

            float centerX = size / 2f;
            float centerY = size / 2f;

            // Title slightly above value
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


            var titleRect = new RectangleF(
                0,
                centerY - titleH / 2f,
                size,
                titleH
            );

            // Draw
            g.DrawString(titleText, fontTitle, brush, titleRect, format);

            float bottomMargin = 4f * uiScale; // 1px scaled

            // Value
            g.DrawString(valueText, fontValue, brush,
                centerX - valueSize.Width / 2f,
                 size - valueSize.Height - bottomMargin);

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
        }

        private static float SweepDegrees(float startDeg, float endDeg)
        {
            // normalize to [0, 360)
            startDeg = Mod360(startDeg);
            endDeg = Mod360(endDeg);

            float sweep = endDeg - startDeg;
            if (sweep < 0)
                sweep += 360f;

            // If you ever want "full ring" when start==end:
            // return sweep == 0 ? 360f : sweep;

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