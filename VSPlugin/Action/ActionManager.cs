
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;
using static SDL3.SDL;

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

            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (button == GamepadButton.Invalid)
                    continue;

                actionToButtonTable[button] = settings.BindAction(button, v =>
                {
                    GAction aEnum = Enum.Parse<GAction>(v);
                    actionToButtonTable[button] = aEnum;
                    ResetButtonBinding(aEnum, button);
                });
            }

            foreach (GamepadAxis axis in Enum.GetValues<GamepadAxis>())
            {
                if (axis == GamepadAxis.Invalid)
                    continue;

                actionToAxisTable[axis] = settings.BindAction(axis, v =>
                {
                    GAction aEnum = Enum.Parse<GAction>(v);
                    actionToAxisTable[axis] = aEnum;
                    ResetAxisBinding(aEnum, axis);
                });
            }
        }

        readonly Dictionary<GamepadButton, GAction> actionToButtonTable = new ();
        readonly Dictionary<GamepadAxis, GAction> actionToAxisTable = new();
        readonly Dictionary<GAction, GamepadButton> buttonBindingTable = new ();
        readonly Dictionary<GAction, GamepadAxis> axisBindingTable = new();


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

        private void ResetButtonBinding(GAction action, GamepadButton button) 
        {
            foreach (var key in buttonBindingTable.Where(kv => kv.Value.Equals(button)).Select(kv => kv.Key).ToList()) 
                buttonBindingTable.Remove(key);

            buttonBindingTable[action]  =button;
        }

        private void ResetAxisBinding(GAction action, GamepadAxis axis)
        {
            foreach (var key in axisBindingTable.Where(kv => kv.Value.Equals(axis)).Select(kv => kv.Key).ToList())
                axisBindingTable.Remove(key);

            axisBindingTable[action] = axis;
        }

        private GamepadState gamepad = null;

        private readonly HUD hud = HUD.Instance;

        internal bool HasActionsOnMainThread() 
        {
          return  actionToButtonTable.Any(pair => gamepad.GetButtonState(pair.Key) == actionTable[pair.Value].Input) || 
          actionToAxisTable.Any(pair => gamepad.GetAxisState(pair.Key) == actionTable[pair.Value].Input);
        } 
        
        internal void ExecuteActionsOnMainThread()
        {
            foreach (var (button, actionEnum) in actionToButtonTable)
            {
                IAction action = actionTable[actionEnum];

                if (gamepad.GetButtonState(button) == actionTable[actionEnum].Input)
                {
                    hud.SetText(action.HUD_Name, 2000);
                    action.Execute();
                }
            }
            foreach (var (axis, actionEnum) in actionToAxisTable)
            {
                IAction action = actionTable[actionEnum];

                if (gamepad.GetAxisState(axis) == actionTable[actionEnum].Input)
                {
                    hud.SetText(action.HUD_Name, 2000);
                    action.Execute();
                }
            }
        }

        internal void Update(GamepadState gamepad) => this.gamepad = gamepad;

        public double Speedmulti { get; private set; }
        public double RotSpeedmulti { get; private set; }

        public float ElevateUp => axisBindingTable.TryGetValue(GAction.ElevatePlus, out var axis) ? gamepad.GetAxisValue(axis) : 0;

        public float ElevateDown => axisBindingTable.TryGetValue(GAction.ElevateMinus, out var axis) ? gamepad.GetAxisValue(axis) : 0;

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (buttonBindingTable.TryGetValue(GAction.TeleportPlus, out var buttonR) && gamepad.GetButtonState(buttonR) == actionTable[GAction.TeleportPlus].Input)
                    jDir = InputY.Up;
                else if (buttonBindingTable.TryGetValue(GAction.TeleportMinus, out var buttonL) && gamepad.GetButtonState(buttonL) == actionTable[GAction.TeleportMinus].Input)
                    jDir = InputY.Down;
                return jDir;

            }
        }
    }
}