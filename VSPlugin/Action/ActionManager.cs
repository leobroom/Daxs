using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());
        public static ActionManager Instance => _instance.Value;
  
        private readonly Dictionary<GamepadButton, Tuple<InputX, IAction>> actionTable = new Dictionary<GamepadButton, Tuple<InputX, IAction>>();

        private double myVar;

        public double SpeedMulti
        {
            get { return myVar; }
            set { myVar = value; }
        }


        public void Register(GamepadButton button, InputX input, IAction dAction)
        {
            Tuple < InputX, IAction > entry =  new(input, dAction);

            if (actionTable.ContainsKey(button))
                actionTable[button] = new Tuple<InputX, IAction>(input, dAction);
            else
                actionTable.Add(button, entry);
        }

        internal void ExecuteActionsOnMainThread(GamepadState state)
        {
            foreach (KeyValuePair<GamepadButton, Tuple<InputX, IAction>> pair in actionTable)
            {
                InputX inputA = GetButtonState(pair.Key, state);
                var tuple = pair.Value;

                if (inputA == tuple.Item1)
                    tuple.Item2.Execute();
            }
        }
        
        private static InputX GetButtonState(GamepadButton button , GamepadState state) 
        {
            return button switch
            {
                GamepadButton.A => state.A,
                GamepadButton.B => state.B,
                GamepadButton.X => state.X,
                GamepadButton.Y => state.Y,
                GamepadButton.Start => state.Start,
                GamepadButton.Back => state.Back,
                GamepadButton.DPadUp => state.DPadUp,
                GamepadButton.DPadDown => state.DPadDown,
                GamepadButton.DPadLeft => state.DPadLeft,
                GamepadButton.DPadRight => state.DPadRight,
                GamepadButton.L1 => state.L1,
                GamepadButton.R1 => state.R1,
                GamepadButton.L3 => state.L3,
                GamepadButton.R3 => state.R3,
                _ => throw new NotImplementedException(),
            };
        }
    }
}