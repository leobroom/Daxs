using Rhino.Display;
using SDL3;
using System;
using System.Drawing;
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

        private const float BaseWidthPx = 100f;
        private const float LeftMarginPx = 80f;
        private const float BottomMarginPx = 18f;

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
            _cachedKey = null;
            Enabled = true;
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

            int x = (int)MathF.Round(LeftMarginPx * uiScale);
            int y = viewport.Size.Height - _cachedGdi.Height - (int)MathF.Round(BottomMarginPx * uiScale);

            dp.DrawBitmap(_cachedDisplay, x, y);
        }

        private void EnsureBitmap(float uiScale)
        {
            float scale = (BaseWidthPx * uiScale) / _assets.Width;

            int w = Math.Max(1, (int)MathF.Round(_assets.Width * scale));
            int h = Math.Max(1, (int)MathF.Round(_assets.Height * scale));

            string key = $"{w}|{h}|{_state.BuildVisualKey()}";
            if (_cachedKey == key && _cachedDisplay != null)
                return;

            _cachedKey = key;

            _cachedDisplay?.Dispose();
            _cachedDisplay = null;

            _cachedGdi?.Dispose();
            _cachedGdi = null;

            _cachedGdi = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using var g = Graphics.FromImage(_cachedGdi);
            g.Clear(System.Drawing.Color.Transparent);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            g.DrawImage(_assets.BaseImage, new Rectangle(0, 0, w, h));

            foreach (var button in _state.ActiveButtons)
            {
                if (!_assets.ButtonMasks.TryGetValue(button, out var mask))
                    continue;

                g.DrawImage(mask, new Rectangle(0, 0, w, h));
            }

            _cachedDisplay = new DisplayBitmap(_cachedGdi);
        }

        public void Dispose()
        {
            Hide();
            _assets.Dispose();
        }
    }
}