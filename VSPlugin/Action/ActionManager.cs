using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());
        public static ActionManager Instance => _instance.Value;
  
        private readonly Dictionary<GButton, Tuple<InputX, IAction>> actionTable = new();

        private readonly Dictionary<AProperty, GButton> stateTable = new();

        private GamepadState state = new();

        public void Register(GButton button, InputX input, IAction dAction)
        {
            Tuple < InputX, IAction > entry =  new(input, dAction);

            if (actionTable.ContainsKey(button))
                actionTable[button] = new Tuple<InputX, IAction>(input, dAction);
            else
                actionTable.Add(button, entry);
        }

        internal void ExecuteActionsOnMainThread()
        {
            foreach (KeyValuePair<GButton, Tuple<InputX, IAction>> pair in actionTable)
            {
                InputX inputA = GetButtonState(pair.Key);
                var tuple = pair.Value;

                if (inputA == tuple.Item1)
                    tuple.Item2.Execute();
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

        internal void Update(GamepadState state)=> this.state = state;

        public void Register(GButton button, AProperty aState)
        {
            if (stateTable.ContainsKey(aState))
                stateTable[aState] = button;
            else
                stateTable.Add(aState, button);
        }

        ////////////////////////

        private readonly double speedmulti =3;

        public double Speedmulti => stateTable.TryGetValue(AProperty.Speedmulti, out var button) && GetButtonState(button) == InputX.IsDown? speedmulti: 1;



        private readonly double rotSpeedmulti = 3;

        public double RotSpeedmulti => stateTable.TryGetValue(AProperty.RotSpeedMulti, out var button) && GetButtonState(button) == InputX.IsDown ? rotSpeedmulti : 1;

        
        public float ElevateUp => stateTable.TryGetValue(AProperty.ElevateUp, out var trigger) ? GetValueState(trigger) : 0;

        public float ElevateDown=> stateTable.TryGetValue(AProperty.ElevateDown, out var trigger) ? GetValueState(trigger) : 0;


        //public InputY Teleport => stateTable.TryGetValue(AProperty., out var trigger) ? GetValueState(trigger) : 0;




        public InputY Teleport
        {
            get 
            {
                InputY jDir = InputY.Default;

                if (stateTable.TryGetValue(AProperty.TeleportUp, out var buttonR) && GetButtonState(buttonR) == InputX.IsDown)
                    jDir = InputY.Up;
                else if (stateTable.TryGetValue(AProperty.TeleportDown, out var buttonL) && GetButtonState(buttonL) == InputX.IsDown)
                    jDir = InputY.Down;
                return jDir;

            }
        }


    }

    public enum AProperty
    {
        Speedmulti,
        RotSpeedMulti,
        ElevateUp,
        ElevateDown,
        TeleportUp,
        TeleportDown
    }
}