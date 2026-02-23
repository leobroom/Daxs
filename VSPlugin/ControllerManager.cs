
using Rhino;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
using System.Drawing;
using System.Reflection;

namespace Daxs
{
    public enum DaxStatus
    {
        NotInitialized = 0,
        Started = 1,
        Stopped = 2
    }

    public sealed class ControllerManager
    {
        public static ControllerManager Instance { get; } = new ControllerManager();


        private IntPtr _gamepadID;

        private ControllerManager()
        {
     
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Daxs.Shared.icon.png"))
            {
                daxsIcon = new Bitmap(stream);
            }

            RhinoApp.Closing += (sender, e) => { settings.SaveSettings(); };

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

        private readonly ActionManager actions = ActionManager.Instance;
        private readonly LayoutManager layout = LayoutManager.Instance;
        private readonly Settings settings = Settings.Instance;
        private readonly HUD hud = HUD.Instance;
        Bitmap daxsIcon = null;


        //Loop
        private CancellationTokenSource _cts;
        private DaxStatus _status = DaxStatus.NotInitialized;

        public DaxStatus State => _status;

        //Gamepad
        Gamepad gamepad = null;

        private readonly object _lock = new();

        public Gamepad CurrentGamepad
        {
            get 
            {
                lock (_lock)
                    return gamepad;
            }
        }

        public void Toggle()
        {
            if (_status == DaxStatus.NotInitialized || _status == DaxStatus.Stopped)
                Start();
            else if (_status == DaxStatus.Started)
                Stop();
        }

        public void Start(bool restart = false)
        {
            if (_status == DaxStatus.Started)
                return;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token), _cts.Token);
            _status = DaxStatus.Started;

            if (!restart) 
            {
                layout.Set(Layout.Fly);
                RhinoApp.WriteLine("Daxs started");
            }
        }

        public void Restart()
        {
            if (_status == DaxStatus.Started)
                return;

            Stop(true);
            Start(true);
        }

        public void Stop(bool restart = false)
        {
            if (_status == DaxStatus.NotInitialized || _status == DaxStatus.Stopped)
                 return;

            _cts.Cancel();
            _status = DaxStatus.Stopped;

            if (!restart)
                RhinoApp.WriteLine("Daxs stopped");
        }

        private static readonly TimeSpan ScanDisconnectedDelay = TimeSpan.FromSeconds(5); // when no pad
        private static readonly TimeSpan ScanFailedOpenDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MinLoopSleep = TimeSpan.FromMilliseconds(1);


        private bool IsConnectedNoLock()=> _gamepadID != IntPtr.Zero && SDL.GamepadConnected(_gamepadID);

        private bool IsConnected()
        {
            lock (_lock)
                return IsConnectedNoLock();
        }

        private void DisconnectAndDispose()
        {
            lock (_lock)
            {
                try { gamepad?.Dispose(); }
                catch { /* swallow: SDL close can fail on teardown */ }

                gamepad = null;
                _gamepadID = IntPtr.Zero;
            }
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
                { gamepad?.Dispose(); } 
                catch { }
                gamepad = new Gamepad(ids[0]);
                _gamepadID = gamepad.GamepadID;
                openedId = _gamepadID;
            }

            return openedId != IntPtr.Zero;
        }

        const double TickHz = 250.0;
        const double TickDt = 1.0 / TickHz;

        private async Task<bool> EnsureConnectedAsync(CancellationToken token)
        {
            // Fast path: already connected
            if (IsConnected())
                return true;

            // If we had an old handle, clean it up
            DisconnectAndDispose();

            // Pump once before scanning
            SDL.PumpEvents();

            // Try to open something
            if (!TryOpenFirstGamepad(out IntPtr openedId))
            {
                // nothing connected -> slow down loop
                await Task.Delay(ScanDisconnectedDelay, token);
                return false;
            }

            // Validate
            if (!SDL.GamepadConnected(openedId))
            {
                RhinoApp.WriteLine($"Failed to open gamepad: {SDL.GetError()}");
                DisconnectAndDispose();
                await Task.Delay(ScanFailedOpenDelay, token);
                return false;
            }

            // Signal once on connect
            SignalConnection(openedId);
            return true;
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
                while (accumulator >= TickDt)
                {
                    accumulator -= TickDt;

                    //hud.TickUiThread();

                    Gamepad gp;
                    IntPtr id;
                    lock (_lock)
                    {
                        gp = gamepad;
                        id = _gamepadID;
                    }

                    // If it got disconnected mid-tick, jump out
                    if (gp == null || id == IntPtr.Zero || !SDL.GamepadConnected(id))
                    {
                        DisconnectAndDispose();
                        break;
                    }

                    gp.Update();
                    actions.Update(gp);
                    layout.Current.HandleInputAndDelta(gp, TickDt);

                }

                double remaining = TickDt - accumulator;
                int sleepMs = (remaining > 0) ? (int)(remaining * 1000.0) : 0;
                if (sleepMs < 1) sleepMs = 1;

                await Task.Delay(TimeSpan.FromMilliseconds(sleepMs), token);
            }

            DisconnectAndDispose();
        }

        private void SignalConnection(nint gamepadID)
        {
           SDL.RumbleGamepad(gamepadID, 30000, 30000, 300);
            SDL.SetGamepadLED(gamepadID, 0, 255, 255);

            string name = GetFriendlyGamepadName(gamepadID);

            string version = Utils.GetPackageVersion();
  

            hud.SetImageToast(daxsIcon, $"DAXS {version} | {name}", 4000);
        }

        public void RumbleGamepad(ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMs) 
        {
            SDL.RumbleGamepad(_gamepadID, lowFrequencyRumble, highFrequencyRumble, durationMs);
        }


        private static string GetFriendlyGamepadName(nint gamepadID)
        {
            // SDL name (often contains "Xbox", "PS5", etc.)
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

            // Vendor-based fallback (common vendor IDs)
            // Microsoft: 0x045E, Sony: 0x054C, Nintendo: 0x057E
            return vendor switch
            {
                0x045E => $"Xbox ({name})",
                0x054C => $"PlayStation ({name})",
                0x057E => $"Nintendo ({name})",
                _ => $"{name} (VID:0x{vendor:X4} PID:0x{product:X4})"
            };
        }

    }
}