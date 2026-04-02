using SDL3;
using System;
using System.Collections.Generic;
using System.Drawing;
using static SDL3.SDL;

namespace Daxs.GUI
{
    internal sealed class GamepadOverlayAssets : IDisposable
    {
        public Bitmap BaseImage { get; }
        public IReadOnlyDictionary<GamepadButton, Bitmap> ButtonMasks => _buttonMasks;

        private readonly Dictionary<GamepadButton, Bitmap> _buttonMasks = new();
        private bool _disposed;

        public int Width => BaseImage.Width;
        public int Height => BaseImage.Height;

        public GamepadOverlayAssets()
        {
            BaseImage = Daxs.Utils.GetSharedBitmap("overlay_inactive.png")
                ?? throw new InvalidOperationException("Failed to load embedded overlay_inactive.png.");

            Load(GamepadButton.Back, "overlay_BACK.png");
            Load(GamepadButton.DPadDown, "overlay_DPAD_DOWN.png");
            Load(GamepadButton.Guide, "overlay_DPAD_GUIDE.png");
            Load(GamepadButton.DPadLeft, "overlay_DPAD_LEFT.png");
            Load(GamepadButton.DPadRight, "overlay_DPAD_RIGHT.png");
            Load(GamepadButton.DPadUp, "overlay_DPAD_UP.png");
            Load(GamepadButton.East, "overlay_EAST.png");
            Load(GamepadButton.LeftShoulder, "overlay_LEFT_SHOULDER.png");
            Load(GamepadButton.LeftStick, "overlay_LEFT_STICK.png");
            Load(GamepadButton.North, "overlay_NORTH.png");
            Load(GamepadButton.RightShoulder, "overlay_RIGHT_SHOULDER.png");
            Load(GamepadButton.RightStick, "overlay_RIGHT_STICK.png");
            Load(GamepadButton.South, "overlay_SOUTH.png");
            Load(GamepadButton.Start, "overlay_START.png");
            Load(GamepadButton.West, "overlay_WEST.png");
        }

        private void Load(GamepadButton button, string fileName)
        {
            var bmp = Daxs.Utils.GetSharedBitmap(fileName);
            if (bmp == null)
                throw new InvalidOperationException($"Failed to load embedded resource '{fileName}'.");

            if (bmp.Width != BaseImage.Width || bmp.Height != BaseImage.Height)
            {
                bmp.Dispose();
                throw new InvalidOperationException(
                    $"Mask '{fileName}' size {bmp.Width}x{bmp.Height} does not match base overlay size {BaseImage.Width}x{BaseImage.Height}.");
            }

            _buttonMasks[button] = bmp;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            BaseImage.Dispose();

            foreach (var bmp in _buttonMasks.Values)
                bmp.Dispose();

            _buttonMasks.Clear();
        }
    }
}