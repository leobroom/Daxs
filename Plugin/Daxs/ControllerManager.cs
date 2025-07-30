// #! csharp
#r "nuget: SharpDX, 4.2.0"
#r "nuget: SharpDX.XInput, 4.2.0"

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.XInput;

using Rhino;
using Rhino.Geometry;
using Rhino.Display;


namespace Daxs
{
    public class ControllerManager
    {
        private static ControllerManager instance = null;

        private ControllerManager()
        {
            RhinoApp.Closing += (sender, e) =>{settings.SaveSettings();};

            RegisterLayout(new FlyLayout());
            RegisterLayout(new MenuLayout());

            InitializeValues();
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

        private Settings settings = Settings.Instance;

        //Buttons
        private DateTime lastPressedTime;
        private string displayMessage = "";
        private GamepadState previousState = new GamepadState();

        IGamepad gamepad = null;

        //Setings values
        double moveSpeed, deadzone, yawSensitivity, pitchSensitivity;

        public enum Status
        {
            NotInitialized =0,
            Started =1,
            Stopped =2
        }

        //Gamepad Layout
        private Dictionary<string, IGamepadLayout> layouts = new();
        private IGamepadLayout currentLayout;

        private void InitializeValues()
        {
            // Unsubscribe first if needed (optional if re-invoked)
            settings["MoveSpeed"].ValueChanged += (s, val) => moveSpeed = val;
            settings["Deadzone"].ValueChanged += (s, val) => deadzone = val;
            settings["YawSensitivity"].ValueChanged += (s, val) => yawSensitivity = val;
            settings["PitchSensitivity"].ValueChanged += (s, val) => pitchSensitivity = val;

            // Set initial values
            moveSpeed = settings["MoveSpeed"].Value;
            deadzone = settings["Deadzone"].Value;
            yawSensitivity = settings["YawSensitivity"].Value;
            pitchSensitivity = settings["PitchSensitivity"].Value;
        }

        public void Toggle()
        {
            if(status == Status.NotInitialized || status == Status.Stopped)
                Start();
            else if (status == Status.Started)
                Stop();
        }

        void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token), _cts.Token);
            status = Status.Started;

            Rhino.Display.DisplayPipeline.DrawForeground += DrawText;    

            SetLayout("Fly");

            RhinoApp.WriteLine("Daxs Start");
        }

        void Stop()
        {
            _cts.Cancel();
            status = Status.Stopped;

            Rhino.Display.DisplayPipeline.DrawForeground -= DrawText;  
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
        #endregion 

        #region LOOP
        async Task  Loop( CancellationToken token)
        {
            RhinoDoc doc =RhinoDoc.ActiveDoc; 
            
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
                    {
                        RhinoApp.WriteLine($"Connected to {gamepad.GetType().Name}");
                    }
                }

                var state = gamepad.GetState();
                var prevStateCopy = previousState;


                // Update the camera on the UI thread.
                Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
                {          


              
                
                if (state.Start && !prevStateCopy.Start)
                {
                    RhinoApp.WriteLine($"START PRESSED UIUI");
                    RhinoApp.RunScript("X_Settings", false);
                    displayMessage = "Start";
                }

                    //RhinoApp.WriteLine("InvokeOnUiThread.");
                    var view = doc.Views.ActiveView;
                    var vp = view.ActiveViewport;   
                    
                    currentLayout?.HandleInput(doc, view, vp, state, prevStateCopy, ref displayMessage, ref lastPressedTime);        

                    if ((DateTime.Now - lastPressedTime).TotalSeconds >= 0.5)
                        displayMessage = "";
                }));


                previousState = state;
                //await Task.Delay(5000, token);
                await Task.Delay(10, token);
                
            }
        }
        #endregion

        #region LAYOUT
        public void RegisterLayout(IGamepadLayout layout)=> layouts[layout.Name] = layout;

        public void SetLayout(string name)
        {
            if (layouts.TryGetValue(name, out var layout))
            {
                currentLayout = layout;
                displayMessage = $"Layout: {name}";
                lastPressedTime = DateTime.Now;
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