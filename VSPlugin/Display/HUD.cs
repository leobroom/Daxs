using Rhino;
using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Eto.Forms;

namespace Daxs
{
    internal sealed class HUD : DisplayConduit
    {
        private static readonly Lazy<HUD> _instance = new(() => new HUD());
        public static HUD Instance => _instance.Value;

        private readonly Settings _settings = Settings.Instance;
        private readonly Stopwatch _sw = new();

        private bool _textVisible;

        // Redraw pacing
        private long _lastRedrawMs;
        private const int RedrawEveryMs = 33; // ~30 fps

        // Elements
        private readonly Dictionary<string, IOverlayElement> _elements = new();

        // Coalesced flush scheduling
        private int _flushScheduled; // 0/1

        // UI timer for animations/timeouts (UI thread)
        private readonly UITimer _uiTimer;

        // -------------------- Latest-wins mailboxes --------------------
        private readonly object _pendingLock = new();

        private ToastRequest? _pendingToast;
        private DonutRequest? _pendingDonut;
        private bool _pendingHideDonut;

        private readonly struct ToastRequest
        {
            public readonly bool IsIcon;
            public readonly string Emoji;
            public readonly string Message;
            public readonly int DurationMs;
            public readonly Bitmap Icon;
            public readonly int IconSizePx;

            public ToastRequest(string emoji, string message, int durationMs)
            {
                IsIcon = false;
                Emoji = emoji;
                Message = message;
                DurationMs = durationMs;
                Icon = null;
                IconSizePx = 0;
            }

            public ToastRequest(Bitmap icon, string message, int durationMs, int iconSizePx)
            {
                IsIcon = true;
                Emoji = null;
                Message = message;
                DurationMs = durationMs;
                Icon = icon;
                IconSizePx = iconSizePx;
            }
        }

        private readonly struct DonutRequest
        {
            public readonly string Title;
            public readonly double Value0to10;
            public readonly double StartDeg;
            public readonly double EndDeg;
            public readonly int DurationMs;

            public DonutRequest(string title, double value0to10, double startDeg, double endDeg, int durationMs)
            {
                Title = title;
                Value0to10 = value0to10;
                StartDeg = startDeg;
                EndDeg = endDeg;
                DurationMs = durationMs;
            }
        }

        private HUD()
        {
            _textVisible = _settings.BindBoolean("TextVisible", v => _textVisible = v);

            _elements["toast"] = new ToastElement();
            _elements["donut"] = new DonutGaugeElement();

            // 30 Hz UI timer
            _uiTimer = new UITimer { Interval = 0.033 };
            _uiTimer.Elapsed += (_, __) => TickUiThread();
        }

        // --------------------------------------------------------------------
        // Public API (thread-safe, latest-wins)
        // --------------------------------------------------------------------

        public void SetText(string emoji, string message, int durationMs = 2000)
        {
            lock (_pendingLock)
            {
                _pendingToast = new ToastRequest(emoji, message, durationMs);
            }
            ScheduleFlush();
        }

        public void SetImageToast(Bitmap icon, string message, int durationMs, int iconSizePx = 20)
        {
            if (icon == null)
                return;

            lock (_pendingLock)
            {
                _pendingToast = new ToastRequest(icon, message, durationMs, iconSizePx);
            }
            ScheduleFlush();
        }

        public void SetDonut(string title, double value0to10, double startDeg, double endDeg, int durationMs = 0)
        {
            lock (_pendingLock)
            {
                _pendingDonut = new DonutRequest(title, value0to10, startDeg, endDeg, durationMs);
                _pendingHideDonut = false; // show beats hide
            }
            ScheduleFlush();
        }

        public void HideDonut()
        {
            lock (_pendingLock)
            {
                _pendingHideDonut = true;
                _pendingDonut = null;
            }
            ScheduleFlush();
        }

        // --------------------------------------------------------------------
        // UI scheduling
        // --------------------------------------------------------------------

        private void ScheduleFlush()
        {
            // Coalesce multiple calls into one UI invocation
            if (Interlocked.Exchange(ref _flushScheduled, 1) == 1)
                return;

            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                _flushScheduled = 0;

                FlushUiThread();

                // Immediate redraw so updates appear ASAP (not waiting for timer tick)
                RequestRedrawUiThread();
            }));
        }

        private void FlushUiThread()
        {
            ToastRequest? toastReq;
            DonutRequest? donutReq;
            bool hideDonut;

            lock (_pendingLock)
            {
                toastReq = _pendingToast;
                donutReq = _pendingDonut;
                hideDonut = _pendingHideDonut;

                _pendingToast = null;
                _pendingDonut = null;
                _pendingHideDonut = false;
            }

            if (!_textVisible)
            {
                HideAllUiThread();
                return;
            }

            // Toast: latest wins
            if (toastReq.HasValue && _elements.TryGetValue("toast", out var tEl) && tEl is ToastElement toast)
            {
                var r = toastReq.Value;

                if (!r.IsIcon)
                    toast.SetText(r.Emoji, r.Message, r.DurationMs);
                else
                    toast.SetIcon(r.Icon, r.Message, r.DurationMs, r.IconSizePx);

                EnsureEnabledUiThread();
            }

            // Donut: hide or latest show
            if (hideDonut)
            {
                if (_elements.TryGetValue("donut", out var dEl) && dEl is DonutGaugeElement donut)
                    donut.Hide();
            }
            else if (donutReq.HasValue && _elements.TryGetValue("donut", out var dEl2) && dEl2 is DonutGaugeElement donut2)
            {
                var r = donutReq.Value;
                donut2.Set(r.Title, r.Value0to10, r.StartDeg, r.EndDeg, r.DurationMs);
                EnsureEnabledUiThread();
            }

            DisableIfNoElementsUiThread();
        }

        // --------------------------------------------------------------------
        // Tick / redraw (UI thread only)
        // --------------------------------------------------------------------

        private void TickUiThread()
        {
            if (!Enabled)
                return;

            long now = _sw.ElapsedMilliseconds;

            bool anyEnabled = false;
            foreach (var el in _elements.Values)
            {
                el.Tick(now);
                if (el.Enabled) anyEnabled = true;
            }

            if (!anyEnabled)
            {
                Enabled = false;
                return;
            }

            if (now - _lastRedrawMs >= RedrawEveryMs)
            {
                _lastRedrawMs = now;
                RequestRedrawUiThread();
            }
        }

        private static void RequestRedrawUiThread()=>  RhinoDoc.ActiveDoc?.Views?.ActiveView?.Redraw();

        // --------------------------------------------------------------------
        // DisplayConduit
        // --------------------------------------------------------------------

        protected override void DrawForeground(DrawEventArgs e)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!Enabled || doc == null)
                return;

            var av = doc.Views.ActiveView;
            if (av == null || e.Viewport.Id != av.ActiveViewportID)
                return;

            long now = _sw.ElapsedMilliseconds;
            float uiScale = UIUtils.GetWindowsScale();

            foreach (var element in _elements.Values)
                if (element.Enabled)
                    element.Draw(e.Display, e.Viewport, uiScale, now);
        }

        protected override void OnEnable(bool enable)
        {
            base.OnEnable(enable);

            if (enable)
            {
                _lastRedrawMs = 0;
                _sw.Restart();

                // Eto note:
                // Some builds use _uiTimer.IsRunning instead of Started.
                if (!_uiTimer.Started)
                    _uiTimer.Start();
            }
            else
            {
                _sw.Stop();

                if (_uiTimer.Started)
                    _uiTimer.Stop();
            }

            RequestRedrawUiThread();
        }

        // --------------------------------------------------------------------
        // Helpers (UI thread)
        // --------------------------------------------------------------------

        private void EnsureEnabledUiThread()
        {
            if (!Enabled)
            {
                _lastRedrawMs = 0;
                Enabled = true; // starts timer in OnEnable
            }
        }

        private void DisableIfNoElementsUiThread()
        {
            foreach (var el in _elements.Values)
                if (el.Enabled)
                    return;

            Enabled = false;
        }

        private void HideAllUiThread()
        {
            foreach (var e in _elements.Values)
                e.Dispose();

            Enabled = false;
        }
    }
}