using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());

        public static ActionManager Instance => _instance.Value;

        public ActionManager() 
        {
            //ActionManager Default
            RegisterAction(GButton.Start, InputX.IsDown, new RhinoCustomAction("_Daxs_Settings", true));
            RegisterAction(GButton.B, InputX.IsDown, new RhinoCustomAction("_ViewCaptureToFile", true));
            RegisterAction(GButton.DPadUp, InputX.IsDown, new SwitchAction());
            RegisterAction(GButton.DPadRight, InputX.IsDown, new LensAction(InputY.Up, 1));
            RegisterAction(GButton.DPadLeft, InputX.IsDown, new LensAction(InputY.Down, 1));
            RegisterAction(GButton.DPadDown, InputX.IsDown, new LensAction(InputY.Default, 35));

            //SpeedMulti
            RegisterState(GButton.L3, InputX.IsDown, "Speedmulti", AProperty.Speedmulti, 3.00);
            RegisterState(GButton.R3, InputX.IsDown, "RotSpeedMulti", AProperty.RotSpeedMulti, 3.00);

            //Elevator
            RegisterState(GButton.L2, InputX.IsDown, "Elevate Down", AProperty.ElevateDown, 1.00);
            RegisterState(GButton.R2, InputX.IsDown, "Elevate Up", AProperty.ElevateUp, 1.00);

            //Teleport
            RegisterState(GButton.L1, InputX.IsDown, "Teleport Down", AProperty.TeleportDown,1.00);
            RegisterState(GButton.R1, InputX.IsDown, "Teleport Up", AProperty.TeleportUp, 1.00);
        }

        private readonly Dictionary<GButton, Tuple<InputX, IAction>> actionTable = new();

        private readonly Dictionary<AProperty, IState> stateTable = new();



        private GamepadState state = new();

        private readonly HUD hud = HUD.Instance;

        public void RegisterAction(GButton button, InputX input, IAction dAction)
        {
            Tuple<InputX, IAction> entry = new(input, dAction);

            if (actionTable.ContainsKey(button))
                actionTable[button] = new Tuple<InputX, IAction>(input, dAction);
            else
                actionTable.Add(button, entry);
        }

        public void RegisterState(GButton button, InputX input, string hudname,  AProperty aState, object value)
        {
            State state = new State(aState, button, input, hudname, value);
            if (stateTable.ContainsKey(aState))
                stateTable[aState] = state;
            else
                stateTable.Add(aState, state);
        }


        internal Dictionary<GButton, Tuple<InputX, IAction>> GetActions()
        {
            return new Dictionary<GButton, Tuple<InputX, IAction>>(actionTable);
        }

        internal void SetActions(Dictionary<GButton, Tuple<InputX, IAction>> newActions)
        {
            actionTable.Clear();

            foreach (var key in newActions.Keys)
                actionTable.Add(key, newActions[key]);
        }


        internal Dictionary<AProperty, IState> GetStates()
        {
            return new Dictionary<AProperty, IState>(stateTable);
        }

        internal void SetStates(Dictionary<AProperty, IState> newStates)
        {
            newStates.Clear();

            foreach (var key in newStates.Keys)
                stateTable.Add(key, newStates[key]);
        }

        internal bool HasActionsOnMainThread()
        {
            foreach (KeyValuePair<GButton, Tuple<InputX, IAction>> pair in actionTable)
            {
                InputX inputA = GetButtonState(pair.Key);
                if (inputA == pair.Value.Item1)
                    return true;
            }
            return false;
        }

        internal void ExecuteActionsOnMainThread()
        {
            foreach (KeyValuePair<GButton, Tuple<InputX, IAction>> pair in actionTable)
            {
                InputX inputA = GetButtonState(pair.Key);
                var tuple = pair.Value;

                if (inputA == tuple.Item1)
                {
                    IAction a = tuple.Item2;
                    hud.SetText(a.HUD_Name, 2000);
                    a.Execute();        
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