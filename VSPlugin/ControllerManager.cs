using System.Threading;
using System.Threading.Tasks;

using Rhino;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
using System;
using static SDL3.SDL;

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

            RhinoApp.WriteLine(sdl3pth);


            NativeLibrary.Load(sdl3pth);

            if (!SDL.Init(SDL.InitFlags.Gamepad))
            {
                RhinoApp.WriteLine($"SDL init failed: {SDL.GetError()}");
                return;
            }else
                RhinoApp.WriteLine($"SDL init success! {SDL.GetVersion()}");

            string dbpth = Utils.GetSharedFile("gamecontrollerdb.txt");
            
            int added = SDL.AddGamepadMappingsFromFile(dbpth);
            RhinoApp.WriteLine($"Loaded {added} SDL Gamepad mappings");
        }

        private readonly ActionManager actions = ActionManager.Instance;
        private readonly LayoutManager layout = LayoutManager.Instance;
        private readonly Settings settings = Settings.Instance;
        private readonly HUD hud = HUD.Instance;

        //Loop
        private CancellationTokenSource _cts;
        private Status status = Status.NotInitialized;



        public enum Status
        {
            NotInitialized = 0,
            Started = 1,
            Stopped = 2
        }

        public void Toggle()
        {
            if (status == Status.NotInitialized || status == Status.Stopped)
                Start();
            else if (status == Status.Started)
                Stop();
        }

        void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token), _cts.Token);
            status = Status.Started;

            layout.Set(Layout.Fly);

            RhinoApp.WriteLine($"Daxs {Utils.GetPackageVersion()} Start");
        }

        void Stop()
        {
            _cts.Cancel();
            status = Status.Stopped;

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
            GamepadState gamepad = null;

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
                        RhinoApp.WriteLine("No gamepad connected.");
                        await Task.Delay(5000, token);
                        continue;
                    }

                    gamepadID = SDL.OpenGamepad(ids[0]);
                    if (gamepadID == IntPtr.Zero)
                    {
                        RhinoApp.WriteLine($"Failed to open gamepad: {SDL.GetError()}");
                        await Task.Delay(5000, token);
                        continue;
                    }

                    string name = SDL.GetGamepadName(gamepadID);
                    ushort vid = SDL.GetGamepadVendor(gamepadID);
                    ushort pid = SDL.GetGamepadProduct(gamepadID);
                    RhinoApp.WriteLine($"Connected to {name} (VID:0x{vid:X4}, PID:0x{pid:X4})");

                    gamepad = new GamepadState(gamepadID);
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

                //RhinoApp.WriteLine("TICK");

                if (SDL.GamepadConnected(gamepadID))
                {
                    gamepad.Update();
                    actions.Update(gamepad);


                    // Update the camera on the UI thread.                    
                    layout.CurrentLayout.HandleInput(gamepad, delta);  



                    //RhinoApp.WriteLine($"🎮 Gamepad: L({gamepad.GetAxisValue(GamepadAxis.LeftX):0.00},{gamepad.GetAxisValue(GamepadAxis.LeftY):0.00})");
                }
                else
                {
                    RhinoApp.WriteLine("Gamepad disconnected — waiting for reconnection...");
                    SDL.CloseGamepad(gamepadID);
                    gamepadID = IntPtr.Zero;
                    gamepad = null;
                }



                //gamepad.Update();
                //actions.Update(gamepad); //https://chatgpt.com/g/g-p-67e9bd1beeac8191a0f9ff9d384c27a1-xboxcontroller/c/68bdc840-3b1c-8321-93cc-6ff4bbe5a5c6

               

                await Task.Delay(1, token);
            }


        }
    }
}