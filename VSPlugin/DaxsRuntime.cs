
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using Rhino;
using SDL3;

using Daxs.Actions;
using Daxs.GUI;
using Daxs.Layout;
using Daxs.Settings;

namespace Daxs
{
    /// <summary>
    /// Status of the Daxs Runtime
    /// </summary>
    public enum DaxStatus
    {
        NotInitialized = 0,
        Started = 1,
        Stopped = 2
    }

    public sealed class DaxsRuntime
    {
        public static DaxsRuntime Instance { get; } = new DaxsRuntime();

        private DaxsRuntime()
        {
            RhinoApp.Closing += (sender, e) => { DaxsConfig.Instance.SaveSettings(); };

            LoadSDL();
        }

        private readonly ActionDispatcher _actions = ActionDispatcher.Instance;
        private readonly LayoutSystem _layout = LayoutSystem.Instance;
        private readonly HUD _hud = HUD.Instance;
        private readonly Bitmap _daxsIcon = Utils.GetSharedBitmap("icon.png");

        //Loop
        private CancellationTokenSource _cts;
        public DaxStatus State { get; private set; } = DaxStatus.NotInitialized;

        //Gamepad
        private IntPtr _gpId;
        private Gamepad _gp = null;
        private readonly object _lock = new();

        // tick timespan
        private const double _tickHz = 250.0;
        private const double _tickDt = 1.0 / _tickHz;

        private static readonly TimeSpan _disconnectedDelay = TimeSpan.FromSeconds(5); // when no gamepad
        private static readonly TimeSpan _failedDelay = TimeSpan.FromSeconds(2);

        public Gamepad CurrentGamepad
        {
            get { lock (_lock) return _gp; }
        }

        /// <summary>
        /// Loads the SD3 Files
        /// </summary>
        private static void LoadSDL() 
        {
            //INIT SDL3
            string sdl3pth = Utils.GetSharedFile("SDL3.dll");
            NativeLibrary.Load(sdl3pth);

            if (!SDL.Init(SDL.InitFlags.Gamepad))
            {
                RhinoApp.WriteLine($"SDL init failed: {SDL.GetError()}");
                return;
            }
            //else
            //    RhinoApp.WriteLine($"SDL init success! {SDL.GetVersion()}");

            string dbpth = Utils.GetSharedFile("gamecontrollerdb.txt");
            int added = SDL.AddGamepadMappingsFromFile(dbpth);
            //RhinoApp.WriteLine($"Loaded {added} SDL Gamepad mappings");
        }

        public void Toggle()
        {
            if (State == DaxStatus.NotInitialized || State == DaxStatus.Stopped)
                Start();
            else if (State == DaxStatus.Started)
                Stop();
        }

        public void Start(bool restart = false)
        {
            if (State == DaxStatus.Started)
                return;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token), _cts.Token);
            State = DaxStatus.Started;

            if (!restart)
            {
                _layout.Set(LayoutType.Fly);
                RhinoApp.WriteLine("Daxs started");
            }
        }

        public void Restart()
        {
            if (State == DaxStatus.Started)
                return;

            Stop(true);
            Start(true);
        }

        public void Stop(bool restart = false)
        {
            if (State == DaxStatus.NotInitialized || State == DaxStatus.Stopped)
                return;

            _cts.Cancel();
            State = DaxStatus.Stopped;

            if (!restart)
                RhinoApp.WriteLine("Daxs stopped");
        }

        async Task Loop(CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            double tickToSec = 1.0 / Stopwatch.Frequency;

            double accumulator = 0;
            long prevTicks = sw.ElapsedTicks;

            SDL.PumpEvents();

            while (!token.IsCancellationRequested)
            {
                long nowTicks = sw.ElapsedTicks;
                double frameDt = (nowTicks - prevTicks) * tickToSec;
                prevTicks = nowTicks;

                // clamp big breaks
                if (frameDt < 0)
                    frameDt = 0;
                if (frameDt > 0.1)
                    frameDt = 0.1;

                bool connected = await EnsureConnectedAsync(token);
                if (!connected)
                {
                    accumulator = 0;
                    continue;
                }

                accumulator += frameDt;

                SDL.PumpEvents();

                // Process at a stable cadence
                while (accumulator >= _tickDt)
                {
                    accumulator -= _tickDt;

                    Gamepad gp;
                    IntPtr id;
                    lock (_lock)
                    {
                        gp = _gp;
                        id = _gpId;
                    }

                    // If it got disconnected mid-tick, jump out
                    if (gp == null || id == IntPtr.Zero || !SDL.GamepadConnected(id))
                    {
                        DisconnectAndDispose();
                        break;
                    }

                    gp.Update();
                    _actions.Update(gp);
                    _hud.Tick(_tickDt);
                    _layout.Current.HandleInputAndDelta(gp, _tickDt);
                }

                double remaining = _tickDt - accumulator;
                int sleepMs = (remaining > 0) ? (int)(remaining * 1000.0) : 0;
                if (sleepMs < 1)
                    sleepMs = 1;

                await Task.Delay(TimeSpan.FromMilliseconds(sleepMs), token);
            }

            DisconnectAndDispose();
        }

        #region Helpers

        private bool IsConnected()
        {
            lock (_lock)
                return _gpId != IntPtr.Zero && SDL.GamepadConnected(_gpId);
        }

        private bool TryOpenFirstGamepad(out IntPtr openedId)
        {
            openedId = IntPtr.Zero;

            uint[] ids = SDL.GetGamepads(out int count);
            if (count <= 0 || ids == null || ids.Length == 0)
                return false;

            lock (_lock)
            {
                try
                { _gp?.Dispose(); }
                catch { }

                _gp = new Gamepad(ids[0]);
                _gpId = _gp.GamepadID;
                openedId = _gpId;
            }

            return openedId != IntPtr.Zero;
        }

        private async Task<bool> EnsureConnectedAsync(CancellationToken token)
        {
            if (IsConnected())
                return true;

            // If we had an old handle, clean it up
            DisconnectAndDispose();

            SDL.PumpEvents();

            // Try to open something
            if (!TryOpenFirstGamepad(out IntPtr openedId))
            {
                // nothing connected -> slow down loop
                await Task.Delay(_disconnectedDelay, token);
                return false;
            }

            // Validate
            if (!SDL.GamepadConnected(openedId))
            {
                RhinoApp.WriteLine($"Failed to open gamepad: {SDL.GetError()}");
                DisconnectAndDispose();
                await Task.Delay(_failedDelay, token);
                return false;
            }

            SignalConnection(openedId);
            return true;
        }

        /// <summary>
        /// Signal once on connect
        /// </summary>
        private void SignalConnection(nint gamepadID)
        {
            SDL.RumbleGamepad(gamepadID, 30000, 30000, 300);
            SDL.SetGamepadLED(gamepadID, 0, 255, 255);

            string name = GetGamepadName(gamepadID);
            string version = Utils.GetPackageVersion();

            _hud.SetImageToast(_daxsIcon, $"DAXS {version} | {name}", 4000);
        }

        public void RumbleGamepad(ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) => SDL.RumbleGamepad(_gpId, lowFrequencyRumble, highFrequencyRumble, durationMs);

        private static string GetGamepadName(nint gamepadID)
        {
            string name = SDL.GetGamepadName(gamepadID);
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown gamepad";

            // Optional: use vendor/product to improve classification
            ushort vendor = SDL.GetGamepadVendor(gamepadID);
            ushort product = SDL.GetGamepadProduct(gamepadID);
            string lower = name.ToLowerInvariant();

            // Heuristics first (works well with most mappings)
            if (lower.Contains("xbox") || lower.Contains("microsoft"))
                return $"Xbox ({name})";
            if (lower.Contains("dualshock") || lower.Contains("dualsense") || lower.Contains("playstation") || lower.Contains("ps4") || lower.Contains("ps5") || lower.Contains("sony"))
                return $"PlayStation ({name})";
            if (lower.Contains("nintendo") || lower.Contains("switch") || lower.Contains("joy-con") || lower.Contains("pro controller"))
                return $"Nintendo ({name})";

            return vendor switch
            {
                0x045E => $"Xbox ({name})",
                0x054C => $"PlayStation ({name})",
                0x057E => $"Nintendo ({name})",
                _ => $"{name} (VID:0x{vendor:X4} PID:0x{product:X4})"
            };
        }

        private void DisconnectAndDispose()
        {
            lock (_lock)
            {
                try { _gp?.Dispose(); }
                catch { }

                _gp = null;
                _gpId = IntPtr.Zero;
            }
        }
        #endregion
    }
}