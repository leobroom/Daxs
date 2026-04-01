

using System.Collections.Generic;
using System.Drawing;

namespace Daxs.GUI
{
    internal abstract class OverlayCommand<TElement> : IOverlayCommand
        where TElement : class, IOverlayElement
    {
        private readonly OverlayIds _elementId;

        protected OverlayCommand(OverlayIds elementId)
        {
            _elementId = elementId;
        }

        public void Apply(HUD hud, IReadOnlyDictionary<OverlayIds, IOverlayElement> elements)
        {
            if (elements.TryGetValue(_elementId, out var element) && element is TElement typed)
                ApplyTo(hud, typed);
        }

        protected abstract void ApplyTo(HUD hud, TElement element);
    }

    internal sealed class SetToastTextCommand : OverlayCommand<ToastElement>
    {
        private readonly string _emoji;
        private readonly string _message;
        private readonly int _durationMs;

        public SetToastTextCommand(string emoji, string message, int durationMs)
            : base(OverlayIds.Toast)
        {
            _emoji = emoji;
            _message = message;
            _durationMs = durationMs;
        }

        protected override void ApplyTo(HUD hud, ToastElement element)
        {
            element.SetText(_emoji, _message, _durationMs);
            hud.EnsureEnabledUiThread();
        }
    }

    internal sealed class SetDonutCommand : OverlayCommand<DonutGaugeElement>
    {
        private readonly string _title;
        private readonly double _value0to10;
        private readonly double _startDeg;
        private readonly double _endDeg;
        private readonly int _durationMs;

        public SetDonutCommand(string title, double value0to10, double startDeg, double endDeg, int durationMs)
            : base(OverlayIds.Donut)
        {
            _title = title;
            _value0to10 = value0to10;
            _startDeg = startDeg;
            _endDeg = endDeg;
            _durationMs = durationMs;
        }

        protected override void ApplyTo(HUD hud, DonutGaugeElement element)
        {
            element.Set(_title, _value0to10, _startDeg, _endDeg, _durationMs);
            hud.EnsureEnabledUiThread();
        }
    }

    internal sealed class HideDonutCommand : OverlayCommand<DonutGaugeElement>
    {
        public HideDonutCommand()
            : base(OverlayIds.Donut)
        {
        }

        protected override void ApplyTo(HUD hud, DonutGaugeElement element)
        {
            element.Hide();
        }
    }

    internal sealed class SetToastIconCommand : OverlayCommand<ToastElement>
    {
        private Bitmap _icon;
        private readonly string _message;
        private readonly int _durationMs;
        private readonly int _iconSizePx;

        public SetToastIconCommand(Bitmap icon, string message, int durationMs, int iconSizePx)
            : base(OverlayIds.Toast)
        {
            _icon = icon != null ? (Bitmap)icon.Clone() : null;
            _message = message;
            _durationMs = durationMs;
            _iconSizePx = iconSizePx;
        }

        protected override void ApplyTo(HUD hud, ToastElement element)
        {
            if (_icon == null)
                return;

            try
            {
                element.SetIcon(_icon, _message, _durationMs, _iconSizePx);
                hud.EnsureEnabledUiThread();
            }
            finally
            {
                _icon.Dispose();
                _icon = null;
            }
        }
    }
}
