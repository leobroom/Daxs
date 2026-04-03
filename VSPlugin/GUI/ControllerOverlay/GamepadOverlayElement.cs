using Rhino.Display;
using SDL3;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using static SDL3.SDL;

namespace Daxs.GUI
{
    internal sealed class GamepadOverlayElement : IOverlayElement
    {
        public string Id => "gamepad_overlay";
        public bool Enabled { get; private set; }

        private readonly GamepadOverlayAssets _assets;
        private GamepadOverlayState _state = new();

        private Bitmap _cachedGdi;
        private DisplayBitmap _cachedDisplay;
        private string _cachedKey;

        // Larger default on-screen size
        private const float BaseWidthPx = 130f;
        private const float RightMarginPx = 18f;
        private const float TopMarginPx = 18f;

        // Real source-image-space positions for overlay_inactive.png (520x360)
        private static readonly PointF LeftStickCenterPx = new(129f, 111f);
        private static readonly PointF RightStickCenterPx = new(324f, 188f);

        // Stick debug / travel
        private const float StickTravelPx = 18f;
        private const float StickDotRadiusPx = 12f;

        // Trigger slider positions on the real artwork

        private static readonly float TrPosDown = 6f;
        private static readonly float TrRelPosUp = 30f;
        private static readonly float TrPosUp = TrPosDown+ TrRelPosUp;

        private static readonly float TrMod1 = 0f;
        private static readonly float TrMod2 = 40f;

        private static readonly PointF LeftTriggerStartPx = new(80f - TrMod1, TrPosUp);
        private static readonly PointF LeftTriggerEndPx = new(177f - TrMod2, TrPosDown);

        private static readonly PointF RightTriggerStartPx = new(343f + TrMod2, TrPosDown);
        private static readonly PointF RightTriggerEndPx = new(441f+ TrMod1, TrPosUp);

        private const float TriggerKnobRadiusPx = 10f;
        private const float TriggerTrackWidthPx = 15f;

        // Temporary debug visuals until placement is tuned
        private const bool DebugVisuals = false;

        private System.Drawing.Color daxsColor = System.Drawing.Color.FromArgb(55, 255, 158);
        private System.Drawing.Color debugColor = System.Drawing.Color.Red;

        public GamepadOverlayElement(GamepadOverlayAssets assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public void Show() => Enabled = true;

        public void Hide()
        {
            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            _cachedKey = null;
            Enabled = false;
        }

        public void SetState(GamepadOverlayState state)
        {
            _state = state?.Clone() ?? new GamepadOverlayState();
            ValidateStateOrThrow(_state);
            _cachedKey = null;
            Enabled = true;
        }

        private static void ValidateStateOrThrow(GamepadOverlayState state)
        {
            if (float.IsNaN(state.LeftStickX) || float.IsInfinity(state.LeftStickX))
                throw new InvalidOperationException("LeftStickX invalid.");
            if (float.IsNaN(state.LeftStickY) || float.IsInfinity(state.LeftStickY))
                throw new InvalidOperationException("LeftStickY invalid.");
            if (float.IsNaN(state.RightStickX) || float.IsInfinity(state.RightStickX))
                throw new InvalidOperationException("RightStickX invalid.");
            if (float.IsNaN(state.RightStickY) || float.IsInfinity(state.RightStickY))
                throw new InvalidOperationException("RightStickY invalid.");
            if (float.IsNaN(state.LeftTrigger) || float.IsInfinity(state.LeftTrigger))
                throw new InvalidOperationException("LeftTrigger invalid.");
            if (float.IsNaN(state.RightTrigger) || float.IsInfinity(state.RightTrigger))
                throw new InvalidOperationException("RightTrigger invalid.");
        }

        public void Tick(long nowMs)
        {
        }

        public void Draw(DisplayPipeline dp, RhinoViewport viewport, float uiScale, long nowMs)
        {
            if (!Enabled)
                return;

            EnsureBitmap(uiScale);

            if (_cachedDisplay == null || _cachedGdi == null)
                return;

            var vp = viewport.Size;

            int marginRight = (int)MathF.Round(RightMarginPx * uiScale);
            int marginTop = (int)MathF.Round(TopMarginPx * uiScale);

            int x = vp.Width - marginRight - _cachedGdi.Width;
            int y = marginTop;

            dp.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(float uiScale)
        {
            float scale = (BaseWidthPx * uiScale) / _assets.Width;

            int w = Math.Max(1, (int)MathF.Round(_assets.Width * scale));
            int h = Math.Max(1, (int)MathF.Round(_assets.Height * scale));

            string key = $"{w}|{h}|{_state.BuildVisualKey()}|dbg={DebugVisuals}";
            if (_cachedKey == key && _cachedDisplay != null)
                return;

            _cachedKey = key;

            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            _cachedGdi = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using var g = Graphics.FromImage(_cachedGdi);
            g.Clear(System.Drawing.Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Base image
            g.DrawImage(_assets.BaseImage, new Rectangle(0, 0, w, h));

            // Active button masks
            foreach (var button in _state.ActiveButtons)
            {
                if (_assets.ButtonMasks.TryGetValue(button, out var mask))
                    g.DrawImage(mask, new Rectangle(0, 0, w, h));
            }

            // Trigger highlight masks
            DrawTriggerMask(g, w, h, GamepadAxis.LeftTrigger, _state.LeftTrigger);
            DrawTriggerMask(g, w, h, GamepadAxis.RightTrigger, _state.RightTrigger);

            // Trigger linear indicators
            DrawTriggerSlider(g, w, h, LeftTriggerStartPx, LeftTriggerEndPx, _state.LeftTrigger, true);
            DrawTriggerSlider(g, w, h, RightTriggerStartPx, RightTriggerEndPx, _state.RightTrigger, false);

            // Stick movement dots
            DrawStickDot(g, w, h, LeftStickCenterPx, _state.LeftStickX, _state.LeftStickY);
            DrawStickDot(g, w, h, RightStickCenterPx, _state.RightStickX, _state.RightStickY);

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
        }

        private void DrawTriggerMask(Graphics g, int w, int h, GamepadAxis axis, float value01)
        {
            if (value01 <= 0.001f)
                return;

            if (!_assets.AxisMasks.TryGetValue(axis, out var mask))
                return;

            using var attrs = new ImageAttributes();
            var matrix = new ColorMatrix
            {
                Matrix33 = Math.Clamp(0.20f + value01 * 0.80f, 0f, 1f)
            };
            attrs.SetColorMatrix(matrix);

            g.DrawImage(
                mask,
                new Rectangle(0, 0, w, h),
                0, 0, mask.Width, mask.Height,
                GraphicsUnit.Pixel,
                attrs);
        }

        private void DrawTriggerSlider(Graphics g, int w, int h, PointF startPx, PointF endPx, float value01, bool left)
        {
            float sx = w / (float)_assets.Width;
            float sy = h / (float)_assets.Height;
            float s = (sx + sy) * 0.5f;

            var p0 = new PointF(startPx.X * sx, startPx.Y * sy);
            var p1 = new PointF(endPx.X * sx, endPx.Y * sy);

            float t = Math.Clamp(value01, 0f, 1f);

            // Left trigger: p0 -> p1
            // Right trigger: mirrored, so p1 -> p0
            var knob = left ? Lerp(p0, p1, t) : Lerp(p1, p0, t);

            using var trackPen = new Pen(debugColor, TriggerTrackWidthPx * s)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            using var activePen = new Pen(daxsColor, TriggerTrackWidthPx * s)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            using var knobBrush = new SolidBrush(daxsColor);
            using var knobPen = new Pen(daxsColor, Math.Max(1f, 1.5f * s));

            if (DebugVisuals)
                g.DrawLine(trackPen, p0, p1);

            if (t > 0.001f)
            {
                if (left)
                    g.DrawLine(activePen, p0, knob);
                else
                    g.DrawLine(activePen, p1, knob);
            }

            float r = TriggerKnobRadiusPx * s;
            if (DebugVisuals || t > 0.001f)
            {
                g.FillEllipse(knobBrush, knob.X - r, knob.Y - r, r * 2f, r * 2f);
                g.DrawEllipse(knobPen, knob.X - r, knob.Y - r, r * 2f, r * 2f);

                DrawCross(g, knob, 8f * s, daxsColor);
            }
        }

        private void DrawStickDot(Graphics g, int w, int h, PointF centerPx, float x01, float y01)
        {
            const float VisualDeadzone = 0.01f;

            // If stick is inside deadzone, do not draw the circle at all.
            if (Math.Abs(x01) <= VisualDeadzone && Math.Abs(y01) <= VisualDeadzone)
                return;

            float sx = w / (float)_assets.Width;
            float sy = h / (float)_assets.Height;
            float s = (sx + sy) * 0.5f;

            var center = new PointF(centerPx.X * sx, centerPx.Y * sy);

            float dx = Math.Clamp(x01, -1f, 1f) * StickTravelPx * s;
            float dy = Math.Clamp(y01, -1f, 1f) * StickTravelPx * s;

            float r = StickDotRadiusPx * s;
            float travelR = StickTravelPx * s;

            using var ringPen = new Pen(debugColor, Math.Max(1f, 1.5f * s));
            using var brush = new SolidBrush(daxsColor);
            using var pen = new Pen(System.Drawing.Color.Black, Math.Max(1f, 1.5f * s));

            if (DebugVisuals)
            {
                g.DrawEllipse(ringPen, center.X - travelR, center.Y - travelR, travelR * 2f, travelR * 2f);
                DrawCross(g, center, 8f * s, debugColor);
            }

            g.FillEllipse(brush, center.X + dx - r, center.Y + dy - r, r * 2f, r * 2f);
            g.DrawEllipse(pen, center.X + dx - r, center.Y + dy - r, r * 2f, r * 2f);
        }
        private static void DrawCross(Graphics g, PointF p, float halfSize, System.Drawing.Color color)
        {
            using var pen = new Pen(color, 2f);
            g.DrawLine(pen, p.X - halfSize, p.Y, p.X + halfSize, p.Y);
            g.DrawLine(pen, p.X, p.Y - halfSize, p.X, p.Y + halfSize);
        }

        private static PointF Lerp(PointF a, PointF b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new PointF(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t);
        }

        public void Dispose()
        {
            Hide();
            _assets.Dispose();
        }
    }
}