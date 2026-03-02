
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Rhino;
using Rhino.Display;
using Daxs.Settings;

namespace Daxs.GUI
{
    internal sealed class OverlayRenderer : DisplayConduit
    {
        private static readonly Lazy<OverlayRenderer> _instance = new(() => new OverlayRenderer());
        public static OverlayRenderer Instance => _instance.Value;

        private readonly DaxsConfig  _settings = DaxsConfig.Instance;
        private readonly Stopwatch _sw = new();

        private bool _textVisible;

        private readonly Dictionary<string, IOverlayElement> _elements = new();         // (UI thread only)

        private readonly object _pendingLock = new();

        private ToastRequest? _pendingToast;
        private DonutRequest? _pendingDonut;
        private bool _pendingHideDonut;

        private int _flushScheduled; 
        private int _uiFrameScheduled;           
        private volatile bool _uiWorkRequested;  

        // Background-thread time accumulator for UI cadence
        private double _sinceLastUi;
        private long _lastRedrawMs;
        private const int TargetFps = 30;
        private const double UiDt = 1.0 / TargetFps;
        private const int RedrawEveryMs = 1000 / TargetFps;

        #region Requests
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

        #endregion

        private OverlayRenderer()
        {
            _textVisible = _settings.BindBoolean("TextVisible", v => _textVisible = v);

            _elements["toast"] = new ToastElement();
            _elements["donut"] = new DonutGaugeElement();
        }

        #region Public API (thread-safe, latest-wins)
     
        public void SetText(string emoji, string message, int durationMs = 2000)
        {
            lock (_pendingLock)
            {
                _pendingToast = new ToastRequest(emoji, message, durationMs);
            }
            _uiWorkRequested = true;
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
            _uiWorkRequested = true;
            ScheduleFlush();
        }

        public void SetDonut(string title, double value0to10, double startDeg, double endDeg, int durationMs = 0)
        {
            lock (_pendingLock)
            {
                _pendingDonut = new DonutRequest(title, value0to10, startDeg, endDeg, durationMs);
                _pendingHideDonut = false; // show beats hide
            }
            _uiWorkRequested = true;
            ScheduleFlush();
        }

        public void HideDonut()
        {
            lock (_pendingLock)
            {
                _pendingHideDonut = true;
                _pendingDonut = null;
            }
            _uiWorkRequested = true;
            ScheduleFlush();
        }
        #endregion

        #region UI scheduling

        private void ScheduleFlush()
        {
            // Coalesce multiple calls into one UI invocation
            if (Interlocked.Exchange(ref _flushScheduled, 1) == 1)
                return;

            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                _flushScheduled = 0;

                FlushUiThread();
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

            // Toast: latest values
            if (toastReq.HasValue &&_elements.TryGetValue("toast", out var tEl) && tEl is ToastElement toast)
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

        #endregion

        #region Tick / redraw

        /// <summary>
        /// Called by ControllerManager with delta seconds (any thread).
        /// This method MUST NOT touch Rhino views or overlay elements directly.
        /// It only schedules UI work.
        /// </summary>
        public void Tick(double delta)
        {
            if (!_textVisible)
                return;

            // If HUD isn’t enabled and no new work is requested, don’t schedule UI frames.
            // (Truth is decided on UI thread; this is just a cheap early-out.)
            if (!Enabled && !_uiWorkRequested)
                return;

            _sinceLastUi += delta;
            if (_sinceLastUi < UiDt)
                return;

            _sinceLastUi = 0.0;

            // Coalesce UI-frame execution
            if (Interlocked.Exchange(ref _uiFrameScheduled, 1) == 1)
                return;

            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                _uiFrameScheduled = 0;

                // Apply pending requests first
                FlushUiThread();

                // Advance animations/timeouts + redraw throttling
                TickUiThread();

                // We processed a UI frame; clear the “requested” flag.
                // (If new requests come in, public API sets it again.)
                _uiWorkRequested = false;
            }));
        }

        /// <summary>
        /// UI thread only "frame": ticks elements and redraws at ~30fps, auto-disables when idle.
        /// </summary>
        private void TickUiThread()
        {
            if (!Enabled)
                return;

            long now = _sw.ElapsedMilliseconds;

            bool anyEnabled = false;
            foreach (var el in _elements.Values)
            {
                el.Tick(now);
                if (el.Enabled)
                    anyEnabled = true;
            }

            if (!anyEnabled)
            {
                Enabled = false; // stops drawing + stopwatch in OnEnable
                return;
            }

            if (now - _lastRedrawMs >= RedrawEveryMs)
            {
                _lastRedrawMs = now;
                RequestRedrawUiThread();
            }
        }

        /// <summary>
        /// ake it visible immediately (don’t wait for next UI cadence)
        /// </summary>
        private static void RequestRedrawUiThread() =>
            RhinoDoc.ActiveDoc?.Views?.ActiveView?.Redraw();
        #endregion

        #region DisplayConduit

        protected override void DrawForeground(DrawEventArgs e)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!Enabled || doc == null)
                return;

            var av = doc.Views.ActiveView;
            if (av == null || e.Viewport.Id != av.ActiveViewportID)
                return;

            long now = _sw.ElapsedMilliseconds;
            float uiScale = GUI_Utils.GetWindowsScale();

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
            }
            else
                _sw.Stop();

            RequestRedrawUiThread();
        }

        #endregion

        #region Helpers

        private void EnsureEnabledUiThread()
        {
            if (!Enabled)
            {
                _lastRedrawMs = 0;
                Enabled = true; // triggers OnEnable(true)
            }
        }

        /// <summary>
        /// When requests were flushed but nothing remains enabled -> disable conduit.
        /// </summary>
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

        #endregion
    }
}