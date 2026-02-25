
using System;
using System.Linq;
using System.Collections.Generic;
using static SDL3.SDL;

namespace Daxs
{
    internal class ActionSystem
    {
        private static readonly Lazy<ActionSystem> _instance = new(() => new ActionSystem());
        public static ActionSystem Instance => _instance.Value;
        private readonly Settings _settings = Settings.Instance;
        private readonly InputGate _gate = InputGate.Instance;

        private readonly Dictionary<GamepadButton, GAction> _actionToButtonTable = new();
        private readonly Dictionary<GamepadAxis, GAction> _actionToAxisTable = new();
        private readonly Dictionary<GAction, GamepadButton> _buttonBindingTable = new();
        private readonly Dictionary<GAction, GamepadAxis> _axisBindingTable = new();

        readonly Dictionary<GAction, IAction> _actionTable = new()
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
            { GAction.ChangeSpeed, new ChangeSpeedModal(InputX.IsHold)}
        };

        //ActionQueue

        private Gamepad _gamepad = null;

        private readonly UniqueQueue<IAction> _actionQueue = new UniqueQueue<IAction>();

        public bool HasActions => _actionQueue.HasValues;

        private readonly HashSet<BaseState> _activeActions = new();
        private readonly HashSet<BaseState> _frameActiveActions = new();

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (_buttonBindingTable.TryGetValue(GAction.TeleportPlus, out var buttonR) && _gamepad.GetButtonState(buttonR) == InputX.IsDown)
                    jDir = InputY.Up;
                else if (_buttonBindingTable.TryGetValue(GAction.TeleportMinus, out var buttonL) && _gamepad.GetButtonState(buttonL) == InputX.IsDown)
                    jDir = InputY.Down;
                return jDir;

            }
        }

        private double _speedMulti = 0, _rotSpeedmulti = 0;

        public double Speedmulti => (_buttonBindingTable.TryGetValue(GAction.Speedmulti, out var button) && _gamepad.GetButtonState(button) == InputX.IsHold) ? _speedMulti : 1;

        public double RotSpeedmulti => (_buttonBindingTable.TryGetValue(GAction.RotSpeedMulti, out var button) && _gamepad.GetButtonState(button) == InputX.IsHold) ? _rotSpeedmulti : 1;

        public double ElevateUp => _axisBindingTable.TryGetValue(GAction.ElevatePlus, out var axis) ? _gamepad.GetAxisValue(axis) : 0;

        public double ElevateDown => _axisBindingTable.TryGetValue(GAction.ElevateMinus, out var axis) ? _gamepad.GetAxisValue(axis) : 0;

        public ActionSystem()
        {
            _speedMulti = _settings.BindNumeric(GAction.Speedmulti, v => _speedMulti = v);
            _rotSpeedmulti = _settings.BindNumeric(GAction.RotSpeedMulti, v => _rotSpeedmulti = v);

            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (button == GamepadButton.Invalid || button == GamepadButton.Count)
                    continue;

                GAction gAction = _settings.BindAction(button, v =>
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

                GAction gAction = _settings.BindAction(axis, v =>
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
                _actionToButtonTable.Remove(button);
            else
                _actionToButtonTable[button] = gAction;
        }

        private void AddToAxisTable(GamepadAxis axis, GAction gAction)
        {
            if (gAction == GAction.Unset)
                _actionToAxisTable.Remove(axis);
            else
                _actionToAxisTable[axis] = gAction;
        }

        private void ResetButtonBinding(GAction action, GamepadButton button) 
        {
            foreach (var key in _buttonBindingTable.Where(kv => kv.Value.Equals(button)).Select(kv => kv.Key).ToList()) 
                _buttonBindingTable.Remove(key);

            _buttonBindingTable[action]  =button;
        }

        private void ResetAxisBinding(GAction action, GamepadAxis axis)
        {
            foreach (var key in _axisBindingTable.Where(kv => kv.Value.Equals(axis)).Select(kv => kv.Key).ToList())
                _axisBindingTable.Remove(key);

            _axisBindingTable[action] = axis;
        }

        internal bool QueueActions()    
        {
            bool hasActions = false;

            _frameActiveActions.Clear();

            foreach (var kvPair in _actionToButtonTable) 
            {
                GamepadButton button = kvPair.Key;
                GAction actionType = kvPair.Value;

                if (_actionTable.TryGetValue(actionType, out IAction action) && 
                    _gamepad.GetButtonState(button) == action.Input &&
                    _gate.Allows(actionType, action)) 
                {
                    hasActions = true;
                    _actionQueue.Enqueue(action);
                    if (action is BaseState bs && bs.WantsDeactivateCallback)
                        _frameActiveActions.Add(bs);
                }
            }

            foreach (var kvPair in _actionToAxisTable)
            {
                GamepadAxis axis = kvPair.Key;
                GAction actionType = kvPair.Value;

                // If no action is registered for this GAction: it's just "activity" (axis moved) detection
                if (!_actionTable.TryGetValue(actionType, out var action))
                {
                    if (_gamepad.GetAxisState(axis) == InputX.IsDown)
                        hasActions = true;

                    continue;
                }

                if (_gamepad.GetAxisState(axis) == action.Input && _gate.Allows(actionType, action))
                {
                    hasActions = true;
                    _actionQueue.Enqueue(action);

                    if (action is BaseState bs && bs.WantsDeactivateCallback)
                        _frameActiveActions.Add(bs);
                }
            }

            // Fire deactivation callbacks for actions that were active but are not anymore
            foreach (var previouslyActive in _activeActions)
                if (!_frameActiveActions.Contains(previouslyActive))
                    previouslyActive.NotifyDeactivated();

            // Update active set for next frame
            _activeActions.Clear();
            foreach (var a in _frameActiveActions)
                _activeActions.Add(a);

            return hasActions;
        }

        internal void ExecuteActionsOnMainThread()
        {
            if(!HasActions)
                return;

            while (_actionQueue.TryDequeue(out var action))
            {
                action.Execute();
            }
        }

        internal void Update(Gamepad gamepad) => this._gamepad = gamepad;
    }
}