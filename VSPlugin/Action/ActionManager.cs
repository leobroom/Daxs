using System;
using System.Collections.Generic;
using System.Linq;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());

        public static ActionManager Instance => _instance.Value;

        public ActionManager()
        {
            ApplyDefaultBindings();
        }


        public void ApplyDefaultBindings()
        {
            // Clear current tables
            actionTable.Clear();
            stateTable.Clear();

            //ActionManager Default
            RegisterAction(new RhinoCustomAction(GButton.Start, InputX.IsDown, "_Daxs_Settings", true));
            RegisterAction(new RhinoCustomAction(GButton.B, InputX.IsDown, "_ViewCaptureToFile", true));
            RegisterAction(new SwitchAction(GButton.DPadUp, InputX.IsDown));
            RegisterAction(new LensAction(GButton.DPadRight, InputX.IsDown, InputY.Up, 1));
            RegisterAction(new LensAction(GButton.DPadLeft, InputX.IsDown, InputY.Down, 1));
            RegisterAction(new LensAction(GButton.DPadDown, InputX.IsDown, InputY.Default, 35));

            //SpeedMulti
            RegisterState(new State(GButton.L3, InputX.IsDown, AProperty.Speedmulti, "Speedmulti", 3.00));
            RegisterState(new State(GButton.R3, InputX.IsDown, AProperty.RotSpeedMulti, "RotSpeedMulti", 3.00));

            //Elevator
            RegisterState(new State(GButton.L2, InputX.IsDown, AProperty.ElevateDown, "Elevate Down", 1.00));
            RegisterState(new State(GButton.R2, InputX.IsDown, AProperty.ElevateUp, "Elevate Up", 1.00));

            //Teleport
            RegisterState(new State(GButton.L1, InputX.IsDown, AProperty.TeleportDown, "Teleport Down", 1.00));
            RegisterState(new State(GButton.R1, InputX.IsDown, AProperty.TeleportUp, "Teleport Up", 1.00));
        }



        private readonly Dictionary<GButton, IAction> actionTable = new();
        private readonly Dictionary<AProperty, IState> stateTable = new();



        private GamepadState state = new();

        private readonly HUD hud = HUD.Instance;

        public void RegisterAction(IAction dAction) => actionTable[dAction.Button] = dAction;

        public void RegisterState(State state) => stateTable[state.Name] = state;

        internal List<IBase> GetActions() => actionTable.Values.Select(a => (IBase)a).ToList();

        internal void SetActions(Dictionary<GButton,  IAction> newActions)
        {
            actionTable.Clear();

            foreach (var kv in newActions)
                actionTable[kv.Key] = kv.Value;
        }

        internal List<IBase> GetStates() => stateTable.Values.Select(a => (IBase)a).ToList();


        internal void SetStates(Dictionary<AProperty, IState> newStates)
        {
            stateTable.Clear();

            foreach (var key in newStates.Keys)
                stateTable[key]= newStates[key];
        }

        internal bool HasActionsOnMainThread() => actionTable.Any(pair => GetButtonState(pair.Key) == pair.Value.Input);
        internal void ExecuteActionsOnMainThread()
        {
            foreach (var (button, action) in actionTable)
            {
                if (GetButtonState(button) == action.Input)
                {
                    hud.SetText(action.HUD_Name, 2000);
                    action.Execute();
                }
            }
        }

        private InputX GetButtonState(GButton button)
        {
            return button switch
            {
                GButton.A => state.A,
                GButton.B => state.B,
                GButton.X => state.X,
                GButton.Y => state.Y,
                GButton.Start => state.Start,
                GButton.Back => state.Back,
                GButton.DPadUp => state.DPadUp,
                GButton.DPadDown => state.DPadDown,
                GButton.DPadLeft => state.DPadLeft,
                GButton.DPadRight => state.DPadRight,
                GButton.L1 => state.L1,
                GButton.R1 => state.R1,
                GButton.L3 => state.L3,
                GButton.R3 => state.R3,
                _ => throw new NotImplementedException(),
            };
        }

        private float GetValueState(GButton button)
        {
            return button switch
            {
                GButton.L2 => state.L2,
                GButton.R2 => state.R2,
                _ => throw new NotImplementedException(),
            };
        }

        internal void Update(GamepadState state) => this.state = state;

        public double Speedmulti => stateTable.TryGetValue(AProperty.Speedmulti, out var state) && GetButtonState(state.Button) == state.Input ? (double)state.Value : 1;

        public double RotSpeedmulti => stateTable.TryGetValue(AProperty.RotSpeedMulti, out var state) && GetButtonState(state.Button) == state.Input ? (double)state.Value : 1;

        public float ElevateUp => stateTable.TryGetValue(AProperty.ElevateUp, out var state) ? GetValueState(state.Button) : 0;

        public float ElevateDown => stateTable.TryGetValue(AProperty.ElevateDown, out var state) ? GetValueState(state.Button) : 0;

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (stateTable.TryGetValue(AProperty.TeleportUp, out var buttonR) && GetButtonState(buttonR.Button) == buttonR.Input)
                    jDir = InputY.Up;
                else if (stateTable.TryGetValue(AProperty.TeleportDown, out var buttonL) && GetButtonState(buttonL.Button) == buttonR.Input)
                    jDir = InputY.Down;
                return jDir;

            }
        }
    }

    public enum AProperty
    {
        Unset,
        Speedmulti,
        RotSpeedMulti,
        ElevateUp,
        ElevateDown,
        TeleportUp,
        TeleportDown,
        DaxSettings,
        Custom,
        Switch,
        Lens
    }
}