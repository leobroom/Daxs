using Rhino;
using Rhino.Display;
using System;
using System.Collections.Concurrent;
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

        // Thread-safe command queue (SetText etc can come from any thread)
        private readonly ConcurrentQueue<Action> _uiCommands = new();

        // Coalesced flush scheduling
        private int _flushScheduled; // 0/1

        // UI timer for animations/timeouts (UI thread)
        private readonly UITimer _uiTimer;

        private HUD()
        {
            _textVisible = _settings.BindBoolean("TextVisible", v => _textVisible = v);

            _elements["toast"] = new ToastElement();
            _elements["donut"] = new DonutGaugeElement();

            // 30 Hz UI timer
            _uiTimer = new UITimer
            {
                Interval = 0.033 // seconds
            };
            _uiTimer.Elapsed += (_, __) => TickUiThread();
        }

        // --------------------------------------------------------------------
        // Public API (thread-safe)
        // --------------------------------------------------------------------

        public void SetText(string emoji, string message, int durationMs = 2000)
        {
            _uiCommands.Enqueue(() =>
            {
                if (!_textVisible)
                {
                    HideAllUiThread();
                    return;
                }

                if (_elements.TryGetValue("toast", out var el) && el is ToastElement toast)
                {
                    toast.SetText(emoji, message, durationMs);
                    EnsureEnabledUiThread();
                }
            });

            ScheduleFlush();
        }

        public void SetImageToast(Bitmap icon, string message, int durationMs, int iconSizePx = 20)
        {
            _uiCommands.Enqueue(() =>
            {
                if (!_textVisible)
                    return;

                if (_elements.TryGetValue("toast", out var el) && el is ToastElement toast)
                {
                    toast.SetIcon(icon, message, durationMs, iconSizePx);
                    EnsureEnabledUiThread();
                }
            });

            ScheduleFlush();
        }

        public void SetDonut(string title, double value0to10, double startDeg, double endDeg, int durationMs = 0)
        {
            _uiCommands.Enqueue(() =>
            {
                if (!_textVisible)
                {
                    HideAllUiThread();
                    return;
                }

                if (_elements.TryGetValue("donut", out var el) && el is DonutGaugeElement donut)
                {
                    donut.Set(title, value0to10, startDeg, endDeg, durationMs);
                    EnsureEnabledUiThread();
                }
            });

            ScheduleFlush();
        }

        public void HideDonut()
        {
            _uiCommands.Enqueue(() =>
            {
                if (_elements.TryGetValue("donut", out var el) && el is DonutGaugeElement donut)
                    donut.Hide();

                DisableIfNoElementsUiThread();
            });

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
            // Apply queued commands (UI thread)
            while (_uiCommands.TryDequeue(out var cmd))
            {
                try { cmd(); }
                catch (Exception ex) { RhinoApp.WriteLine($"HUD command failed: {ex.Message}"); }
            }
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

        private static void RequestRedrawUiThread()
        {
            RhinoDoc.ActiveDoc?.Views?.ActiveView?.Redraw();
        }

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
            // Keep elements registered; just hide/clear their state
            foreach (var e in _elements.Values)
                e.Dispose();

            Enabled = false;
        }
    }
}