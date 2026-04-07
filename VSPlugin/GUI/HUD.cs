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
    internal sealed class HUD : DisplayConduit
    {
        private static readonly Lazy<HUD> _instance = new(() => new HUD());
        public static HUD Instance => _instance.Value;

        private readonly DaxsConfig _settings = DaxsConfig.Instance;
        private readonly Stopwatch _sw = new();

        private bool _textVisible;

        // UI thread only
        private readonly Dictionary<OverlayIds, IOverlayElement> _elements = new();

        // Cross-thread command queue
        private readonly object _pendingLock = new();
        private readonly Queue<IOverlayCommand> _pendingCommands = new();

        private int _flushScheduled;
        private int _uiFrameScheduled;
        private volatile bool _uiWorkRequested;

        // Background-thread time accumulator for UI cadence
        private double _sinceLastUi;
        private long _lastRedrawMs;

        private const int TargetFps = 30;
        private const double UiDt = 1.0 / TargetFps;
        private const int RedrawEveryMs = 1000 / TargetFps;

        private HUD()
        {
            _textVisible = _settings.BindBoolean("TextVisible", v => _textVisible = v);

            _elements[OverlayIds.Toast] = new ToastElement();
            _elements[OverlayIds.Donut] = new DonutGaugeElement();
            _elements[OverlayIds.GamepadOverlay] = new GamepadOverlayElement(new GamepadOverlayAssets());
        }

        #region Public API

        public void SetText(string emoji, string message, int durationMs = 2000)=> EnqueueCommand(new SetToastTextCommand(emoji, message, durationMs));

        public void SetImageToast(Bitmap icon, string message, int durationMs, int iconSizePx = 20)=> EnqueueCommand(new SetToastIconCommand(icon, message, durationMs, iconSizePx));

        public void SetDonut(string title, double value0to10, double startDeg, double endDeg, int durationMs = 0)=> EnqueueCommand(new SetDonutCommand(title, value0to10, startDeg, endDeg, durationMs));

        public void HideDonut()=> EnqueueCommand(new HideDonutCommand());

        public void SetGamepadOverlay(Gamepad state)=>   EnqueueCommand(new SetGamepadOverlayCommand(state));

        public void HideGamepadOverlay() =>  EnqueueCommand(new HideGamepadOverlayCommand());

        #endregion

        #region Command queue

        private void EnqueueCommand(IOverlayCommand command)
        {
            if (command == null)
                return;

            lock (_pendingLock)
            {
                _pendingCommands.Enqueue(command);
            }

            _uiWorkRequested = true;
            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
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
            List<IOverlayCommand> commands = null;

            lock (_pendingLock)
            {
                if (_pendingCommands.Count > 0)
                {
                    commands = new List<IOverlayCommand>(_pendingCommands.Count);

                    while (_pendingCommands.Count > 0)
                        commands.Add(_pendingCommands.Dequeue());
                }
            }

            if (!_textVisible)
            {
                Disable();
                return;
            }

            if (commands != null)
            {
                foreach (var command in commands)
                {
                    try
                    {
                        command.Apply(this, _elements);
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"HUD command failed: {ex.Message}");
                    }
                }
            }

            DisableIfNoElementsUiThread();
        }

        #endregion

        #region Tick / redraw

        /// <summary>
        /// Called by Runtime with delta seconds.
        /// This method MUST NOT touch Rhino views or overlay elements directly.
        /// </summary>
        public void Tick(double delta)
        {
            if (!_textVisible)
                return;

            if (!Enabled && !_uiWorkRequested)
                return;

            _sinceLastUi += delta;
            if (_sinceLastUi < UiDt)
                return;

            _sinceLastUi = 0.0;

            if (Interlocked.Exchange(ref _uiFrameScheduled, 1) == 1)
                return;

            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                _uiFrameScheduled = 0;

                FlushUiThread();
                TickUiThread();

                _uiWorkRequested = false;
            }));
        }

        /// <summary>
        /// UI thread only frame: ticks elements and redraws at ~30fps, auto-disables when idle.
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
                Enabled = false;
                return;
            }

            if (now - _lastRedrawMs >= RedrawEveryMs)
            {
                _lastRedrawMs = now;
                RequestRedrawUiThread();
            }
        }

        private static void RequestRedrawUiThread() =>
            RhinoDoc.ActiveDoc?.Views?.ActiveView?.Redraw();

        #endregion

        #region DisplayConduit

        protected override void DrawForeground(DrawEventArgs e)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!Enabled || doc == null)
                return;

            var av = doc.Views?.ActiveView;
            if (av == null || e.Viewport == null ||e.Viewport.Id != av.ActiveViewportID)
                return;

            long now = _sw.ElapsedMilliseconds;
            float uiScale = GUI_Utils.GetWindowsScale();

            foreach (var element in _elements.Values)
            {
                if (element.Enabled)
                    element.Draw(e.Display, e.Viewport, uiScale, now);
            }

           // e.Display.Draw2dText("conduit ON",System.Drawing.Color.Red,new Rhino.Geometry.Point2d(20, 40), false, 20);
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
            {
                _sw.Stop();
            }

            RequestRedrawUiThread();
        }

        #endregion

        #region Internal helpers used by commands

        internal void EnsureEnabledUiThread()
        {
            if (!Enabled)
            {
                _lastRedrawMs = 0;
                Enabled = true;
            }
        }

        private void DisableIfNoElementsUiThread()
        {
            foreach (var el in _elements.Values)
            {
                if (el.Enabled)
                    return;
            }

            Enabled = false;
        }

        public void Hide()
        {
            foreach (var element in _elements.Values)
            {
                if (element is GamepadOverlayElement overlay)
                    overlay.Hide();
                else if (element is ToastElement toast)
                    toast.Hide();
                else if (element is DonutGaugeElement donut)
                    donut.Hide();
            }
        }

        private void Disable()
        {
            foreach (var element in _elements.Values)
                element.Dispose();

            Enabled = false;
        }

        #endregion
    }

    internal enum OverlayIds
    {
        Toast,
        Donut,
        GamepadOverlay
    }

    internal interface IOverlayCommand
    {
        void Apply(HUD hud, IReadOnlyDictionary<OverlayIds, IOverlayElement> elements);
    }
}