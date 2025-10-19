
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());
        public static ActionManager Instance => _instance.Value;
        private readonly Settings settings = Settings.Instance;
        public ActionManager()
        {
            Speedmulti = settings.BindNumeric(GAction.Speedmulti, v => Speedmulti = v);
            RotSpeedmulti = settings.BindNumeric(GAction.RotSpeedMulti, v => RotSpeedmulti = v);

            foreach (GButton button in Enum.GetValues<GButton>())
            {
                if (button == GButton.Unset)
                    continue;

                actionBindingTable[button] = settings.BindAction(button, v =>
                {
                    GAction aEnum = Enum.Parse<GAction>(v);
                    actionBindingTable[button] = aEnum;
                    ResetButtonBinding(aEnum, button);
                });
            }

        }

        readonly Dictionary<GButton, GAction> actionBindingTable = new ();
        readonly Dictionary<GAction, GButton> buttonBindingTable = new ();
        readonly Dictionary<GAction, IAction> actionTable = new ()
        {
            { GAction.C1,        new RhinoCustomAction(InputX.IsDown,GAction.C1)},
            { GAction.C2,        new RhinoCustomAction(InputX.IsDown,GAction.C2)},
            { GAction.C3,        new RhinoCustomAction(InputX.IsDown,GAction.C3) },
            { GAction.C4,        new RhinoCustomAction(InputX.IsDown,GAction.C4) },
            { GAction.C5,        new RhinoCustomAction(InputX.IsDown,GAction.C5) },
            { GAction.C6,        new RhinoCustomAction(InputX.IsDown,GAction.C6) },
            { GAction.LensPlus,  new LensAction(InputX.IsDown,InputY.Up ) },
            { GAction.LensMinus, new LensAction( InputX.IsDown,InputY.Down) },
            { GAction.LensDefault,new LensAction(InputX.IsDown,InputY.Default ) },
            { GAction.SwitchMode, new SwitchAction(InputX.IsDown) },
        };

        private void ResetButtonBinding(GAction action, GButton button) 
        {
            foreach (var key in buttonBindingTable.Where(kv => kv.Value.Equals(button)).Select(kv => kv.Key).ToList()) 
                buttonBindingTable.Remove(key);

            buttonBindingTable[action]  =button;
        }

        private GamepadState state = new();

        private readonly HUD hud = HUD.Instance;

        internal bool HasActionsOnMainThread() => actionBindingTable.Any(pair => GetButtonState(pair.Key) == actionTable[pair.Value].Input);
        
        internal void ExecuteActionsOnMainThread()
        {
            foreach (var (button, actionEnum) in actionBindingTable)
            {
                IAction action = actionTable[actionEnum];

                if (GetButtonState(button) == actionTable[actionEnum].Input)
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

        public double Speedmulti { get; private set; }
        public double RotSpeedmulti { get; private set; }

        public float ElevateUp => buttonBindingTable.TryGetValue(GAction.ElevatePlus, out var button) ? GetValueState(button) : 0;

        public float ElevateDown => buttonBindingTable.TryGetValue(GAction.ElevateMinus, out var button) ? GetValueState(button) : 0;

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (buttonBindingTable.TryGetValue(GAction.TeleportPlus, out var buttonR) && GetButtonState(buttonR) == actionTable[GAction.TeleportPlus].Input)
                    jDir = InputY.Up;
                else if (buttonBindingTable.TryGetValue(GAction.TeleportMinus, out var buttonL) && GetButtonState(buttonL) == actionTable[GAction.TeleportMinus].Input)
                    jDir = InputY.Down;
                return jDir;

            }
        }
    }
}