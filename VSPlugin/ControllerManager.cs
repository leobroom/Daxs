using System.Threading;
using System.Threading.Tasks;
using Rhino;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
using System;

namespace Daxs
{
    public sealed class ControllerManager
    {
        public static ControllerManager Instance { get; } = new ControllerManager();

        private ControllerManager()
        {
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

        //Loop
        private CancellationTokenSource _cts;
        private DaxStatus status = DaxStatus.NotInitialized;

        public DaxStatus State => status;

        //Gamepad
        Gamepad gamepad = null;

        private readonly object _lock = new();

        public Gamepad CurrentGamepad
        {
            get 
            {
                lock (_lock)
                {
                    return gamepad;
                }
            }
        }

        public enum DaxStatus
        {
            NotInitialized = 0,
            Started = 1,
            Stopped = 2
        }

        public void Toggle()
        {
            if (status == DaxStatus.NotInitialized || status == DaxStatus.Stopped)
                Start();
            else if (status == DaxStatus.Started)
                Stop();
        }

        void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token), _cts.Token);
            status = DaxStatus.Started;

            layout.Set(Layout.Fly);

            RhinoApp.WriteLine("Daxs Start");
        }

        void Stop()
        {
            _cts.Cancel();
            status = DaxStatus.Stopped;

            RhinoApp.WriteLine("Daxs Stop");
        }

        async Task Loop(CancellationToken token)
        {
            //dalta
            var sw = Stopwatch.StartNew();
            long prevTicks = sw.ElapsedTicks;
            double tickToSec = 1.0 / Stopwatch.Frequency;

            //SDL

            SDL.PumpEvents();

            IntPtr gamepadID = IntPtr.Zero;

            //Whileloop
            while (!token.IsCancellationRequested)
            {
                SDL.PumpEvents();

                // If no gamepad or disconnected - try to (re)connect
                if (gamepadID == IntPtr.Zero || !SDL.GamepadConnected(gamepadID))
                {
                    // Try to find the first available controller
                    uint[] ids = SDL.GetGamepads(out int count);
                    if (count == 0)
                    {
                        if (gamepad!= null)
                        {
                            lock (_lock)
                            {
                                gamepad?.Dispose();
                                gamepad = null;
                            }

                            gamepadID = IntPtr.Zero;
                        }

                        //RhinoApp.WriteLine("No gamepad connected.");
                        await Task.Delay(5000, token);
                        continue;
                    }
                    lock (_lock)
                    {
                        gamepad = new Gamepad(ids[0]);
                    }
                    gamepadID = gamepad.GamepadID;
           
                    if (gamepadID == IntPtr.Zero)
                    {
                        RhinoApp.WriteLine($"Failed to open gamepad: {SDL.GetError()}");
                        await Task.Delay(5000, token);
                        continue;
                    }

               
                }

                long now = sw.ElapsedTicks;
                double delta = (now - prevTicks) * tickToSec;

                // clamp to avoid huge jumps 
                if (delta < 1e-5)
                    delta = 1e-5;   // min ~0.01 ms
                if (delta > 0.05)
                    delta = 0.05;   // max 50 ms (~20 FPS)

                prevTicks = now;

                hud.Tick();

                if (SDL.GamepadConnected(gamepadID))
                {
                    gamepad.Update();
                    actions.Update(gamepad);
                
                    layout.Current.HandleInput(gamepad, delta);  
                }

                await Task.Delay(1, token);
            }
        } 
    }
}