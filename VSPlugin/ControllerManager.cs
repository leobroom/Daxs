using System;
using System.Threading;
using System.Threading.Tasks;

using Rhino;
using Rhino.Geometry;
using Rhino.Display;

namespace Daxs
{
    public class ControllerManager
    {
        private static ControllerManager instance = null;

        private ActionManager actionManager = ActionManager.Instance;
        private ControllerManager()
        {
            RhinoApp.Closing += (sender, e) => { settings.SaveSettings(); };

            layoutManager.RegisterLayout(new FlyLayout());
            layoutManager.RegisterLayout(new WalkLayout());
            layoutManager.RegisterLayout(new MenuLayout());

            layoutManager.Message += (sender, e) => SetMessage(e.Message);

            //Test
            //actionManager.RegisterBinding(GamepadButton.Start, new RhinoCmdAction("Daxs_Settings", true));
            //actionManager.RegisterBinding(GamepadButton.B, new RhinoCmdAction("_ViewCaptureToFile", true));
            //actionManager.RegisterBinding(GamepadButton.DPadUp, new SwitchAction());
            //actionManager.RegisterBinding(GamepadButton.DPadRight, new LensAction("Up"));
            //actionManager.RegisterBinding(GamepadButton.DPadLeft, new LensAction("Down"));
            //actionManager.RegisterBinding(GamepadButton.DPadDown, new LensAction("Reset"));
            //actionManager.RegisterBinding(GamepadButton.LR2, new EscalatorAction());
            //actionManager.RegisterBinding(GamepadButton.LR1, new TeleportAction());
        }

        public static ControllerManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ControllerManager();

                return instance;
            }
        }

        private CancellationTokenSource _cts;
        private Status status = Status.NotInitialized;

        private readonly Settings settings = Settings.Instance;

        //Buttons
        private DateTime lastPressedTime;
        private string displayMessage = "";
        private GamepadState previousState = new GamepadState();

        IGamepad gamepad = null;

        LayoutManager layoutManager = LayoutManager.Instance;

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

            DisplayPipeline.DrawForeground += DrawText;

            layoutManager.SetLayout("Fly");

            RhinoApp.WriteLine($"Daxs {Utils.GetPackageVersion()} Start");
        }

        void Stop()
        {
            _cts.Cancel();
            status = Status.Stopped;

            DisplayPipeline.DrawForeground -= DrawText;
            RhinoApp.WriteLine("Daxs Stop");
        }

        #region DISPLAY
        void DrawText(object sender, DrawEventArgs e)
        {
            var activeView = RhinoDoc.ActiveDoc.Views.ActiveView;
            if (e.Viewport.Id != activeView.MainViewport.Id)
                return;

            var screenPoint = new Point2d(50, 50);
            e.Display.Draw2dText(displayMessage, System.Drawing.Color.Black, screenPoint, false, 25);
        }

        internal void SetMessage(string msg)
        {
            displayMessage = msg;
            lastPressedTime = DateTime.Now;
        }

        #endregion

        #region LOOP
        async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Try to (re)initialize the controller
                if (gamepad == null || !gamepad.IsConnected)
                {
                    gamepad = TryGetGamepad();
                    if (gamepad == null)
                    {
                        RhinoApp.WriteLine("No supported controller found.");
                        await Task.Delay(1000, token);
                        continue;
                    }
                    else
                        RhinoApp.WriteLine($"Connected to {gamepad.GetType().Name}");
                }

                var state = gamepad.GetState();
                var prevStateCopy = previousState;

                // Update the camera on the UI thread.                    
                layoutManager.CurrentLayout?.HandleInput(state, prevStateCopy);

                if ((DateTime.Now - lastPressedTime).TotalSeconds > 2)
                    displayMessage = "";

                previousState = state;
                await Task.Delay(10, token);
            }
        }
        #endregion

        private IGamepad TryGetGamepad()
        {
            var xbox = new XboxGamepad();
            if (xbox.IsConnected)
                return xbox;
            else
            {
                var ps4 = new PS4Gamepad();
                if (ps4.IsConnected)
                    return ps4;
            }

            return null;
        }
    }
}