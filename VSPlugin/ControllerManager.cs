using System.Threading;
using System.Threading.Tasks;

using Rhino;
using System.Diagnostics;

namespace Daxs
{
    public sealed class ControllerManager
    {
        public static ControllerManager Instance { get; } = new ControllerManager();

        private ControllerManager()
        {
            RhinoApp.Closing += (sender, e) => { settings.SaveSettings(); };
        }

        private readonly ActionManager actions = ActionManager.Instance;
        private readonly LayoutManager layout = LayoutManager.Instance;
        private readonly Settings settings = Settings.Instance;
        private readonly HUD hud = HUD.Instance;

        //Loop
        private CancellationTokenSource _cts;
        private Status status = Status.NotInitialized;

        private IGamepad gamepad = null;

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

            //Whileloop
            while (!token.IsCancellationRequested)
            {
                // Try to (re)initialize the controller
                if (gamepad == null || !gamepad.IsConnected)
                {
                    gamepad = Gamepad.TryGetGamepad();
                    if (gamepad == null)
                    {
                        RhinoApp.WriteLine("No supported controller found.");
                        await Task.Delay(1000, token);
                        continue;
                    }
                    else
                        RhinoApp.WriteLine($"Connected to {gamepad.GetType().Name}");
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

                var state = gamepad.GetState();
                actions.Update(state); //https://chatgpt.com/g/g-p-67e9bd1beeac8191a0f9ff9d384c27a1-xboxcontroller/c/68bdc840-3b1c-8321-93cc-6ff4bbe5a5c6

                // Update the camera on the UI thread.                    
                layout.CurrentLayout.HandleInput(state,delta);

                await Task.Delay(1, token);
            }
        }
    }
}