
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;
using static SDL3.SDL;
using System.Runtime.CompilerServices;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());
        public static ActionManager Instance => _instance.Value;
        private readonly Settings settings = Settings.Instance;
        public ActionManager()
        {
            speedMulti = settings.BindNumeric(GAction.Speedmulti, v => speedMulti = v);
            rotSpeedmulti = settings.BindNumeric(GAction.RotSpeedMulti, v => rotSpeedmulti = v);
   

            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (button == GamepadButton.Invalid || button == GamepadButton.Count)
                    continue;

                GAction gAction = settings.BindAction(button, v =>
                {
                    GAction aEnum = Enum.Parse<GAction>(v);
 
                    AddToButtonTable(button, aEnum);
                    ResetButtonBinding(aEnum, button);
                });

                AddToButtonTable(button,  gAction);
            }

            foreach (GamepadAxis axis in Enum.GetValues<GamepadAxis>())
            {
                if (axis == GamepadAxis.Invalid || axis == GamepadAxis.Count)
                    continue;

                GAction gAction = settings.BindAction(axis, v =>
                {
                    GAction aEnum = Enum.Parse<GAction>(v);
                    AddToAxisTable(axis, aEnum);
                    ResetAxisBinding(aEnum, axis);
                });

                AddToAxisTable(axis,  gAction);
            }
        }

        private void AddToButtonTable(GamepadButton button, GAction gAction) 
        {
            if (gAction == GAction.Unset)
                actionToButtonTable.Remove(button);
            else
                actionToButtonTable[button] = gAction;
        }

        private void AddToAxisTable(GamepadAxis axis, GAction gAction)
        {
            if (gAction == GAction.Unset)
                actionToAxisTable.Remove(axis);
            else
                actionToAxisTable[axis] = gAction;
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
            { GAction.NextView, new NextView(InputX.IsDown)},
            { GAction.NextViewport, new NextViewport(InputX.IsDown)},
            { GAction.NextDisplaymode, new NextDisplaymode(InputX.IsDown)},
            { GAction.NextNamedView, new NextNamedView(InputX.IsDown)},
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

        private Gamepad gamepad = null;

        private readonly HUD hud = HUD.Instance;


        private readonly UniqueQueue<IAction> actionQueue = new UniqueQueue<IAction>();

        public bool HasActions=>  actionQueue.HasValues; 



        internal bool QueueActions()    
        {
            bool hasActions = false;

            foreach (var kvPair in actionToButtonTable) 
            {
                GamepadButton button = kvPair.Key;
                GAction actionType = kvPair.Value;

                if (actionTable.TryGetValue(actionType, out IAction action) && gamepad.GetButtonState(button) == action.Input) 
                {
                    hasActions = true;
                    actionQueue.Enqueue(action);
                }
            }

            foreach (var kvPair in actionToAxisTable)
            {
                GamepadAxis axis = kvPair.Key;
                GAction actionType = kvPair.Value;

                if (!actionTable.TryGetValue(kvPair.Value, out var action))
                {
                    if (gamepad.GetAxisState(axis) == InputX.IsDown)
                        hasActions = true;
                }
                else if(gamepad.GetAxisState(axis) == action.Input)
                {
                    hasActions = true;
                    actionQueue.Enqueue(action);
                }
            }

            return hasActions;
        }

        internal void ExecuteActionsOnMainThread()
        {
            if(!HasActions)
                return;

            while (actionQueue.TryDequeue(out var action))
            {
                if (action is ICalculate)
                     ((ICalculate)action).Calculate(); 

                hud.SetText(action.HUD_Emoji, action.HUD_Text, 2000);
                action.Execute();
            }
        }

        internal void Update(Gamepad gamepad) => this.gamepad = gamepad;


        public double Speedmulti=> (buttonBindingTable.TryGetValue(GAction.Speedmulti, out var button) && gamepad.GetButtonState(button) == InputX.IsHold)  ? speedMulti : 1;
        double speedMulti = 0;

        public double RotSpeedmulti=> (buttonBindingTable.TryGetValue(GAction.RotSpeedMulti, out var button) && gamepad.GetButtonState(button) == InputX.IsHold) ? rotSpeedmulti : 1;

        double rotSpeedmulti = 0;

        public double ElevateUp => axisBindingTable.TryGetValue(GAction.ElevatePlus, out var axis) ? gamepad.GetAxisValue(axis)  : 0;

        public double ElevateDown => axisBindingTable.TryGetValue(GAction.ElevateMinus, out var axis) ? gamepad.GetAxisValue(axis)  : 0;

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (buttonBindingTable.TryGetValue(GAction.TeleportPlus, out var buttonR) && gamepad.GetButtonState(buttonR) == InputX.IsDown)
                    jDir = InputY.Up;
                else if (buttonBindingTable.TryGetValue(GAction.TeleportMinus, out var buttonL) && gamepad.GetButtonState(buttonL) == InputX.IsDown)
                    jDir = InputY.Down;
                return jDir;

            }
        }
    }
}