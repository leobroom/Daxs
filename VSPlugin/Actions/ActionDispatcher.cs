using System;
using System.Linq;
using System.Collections.Generic;

using static SDL3.SDL;
using Daxs.Settings;

namespace Daxs.Actions
{
    internal class ActionDispatcher
    {
        private static readonly Lazy<ActionDispatcher> _instance = new(() => new ActionDispatcher());
        public static ActionDispatcher Instance => _instance.Value;
        private readonly ActionGate _gate = ActionGate.Instance;

        private readonly Dictionary<GamepadButton, BindingId> _actionToButtonTable = new();
        private readonly Dictionary<GamepadAxis, BindingId> _actionToAxisTable = new();
        private readonly Dictionary<BindingId, GamepadButton> _buttonBindingTable = new();
        private readonly Dictionary<BindingId, GamepadAxis> _axisBindingTable = new();

        readonly Dictionary<BindingId, IAction> _actionTable = new()
        {
            { BindingId.Macro1, new RhinoMacroAction(InputX.IsDown,BindingId.Macro1)},
            { BindingId.Macro2, new RhinoMacroAction(InputX.IsDown,BindingId.Macro2)},
            { BindingId.Macro3, new RhinoMacroAction(InputX.IsDown,BindingId.Macro3) },
            { BindingId.Macro4, new RhinoMacroAction(InputX.IsDown,BindingId.Macro4) },
            { BindingId.Macro5, new RhinoMacroAction(InputX.IsDown,BindingId.Macro5) },
            { BindingId.Macro6, new RhinoMacroAction(InputX.IsDown,BindingId.Macro6) },
            { BindingId.LensPlus, new LensAction(InputX.IsDown,InputY.Up ) },
            { BindingId.LensMinus, new LensAction( InputX.IsDown,InputY.Down) },
            { BindingId.LensDefault, new LensAction(InputX.IsDown,InputY.Default ) },
            { BindingId.SwitchMode, new SwitchAction(InputX.IsDown) },
            { BindingId.NextView, new NextViewAction(InputX.IsDown)},
            { BindingId.NextViewport, new ViewportAction(InputX.IsDown)},
            { BindingId.NextDisplaymode, new DisplaymodeAction(InputX.IsDown)},
            { BindingId.NextNamedView, new NamedViewAction(InputX.IsDown)},
            { BindingId.ChangeSpeed, new ChangeSpeedAction(InputX.IsHold)}
        };

        //ActionQueue

        private Gamepad _gamepad = null;

        private readonly UniqueQueue<IAction> _actionQueue = new UniqueQueue<IAction>();

        public bool HasActions => _actionQueue.HasValues;

        private readonly HashSet<ActionBase> _activeActions = new();
        private readonly HashSet<ActionBase> _frameActiveActions = new();

        public InputY Teleport
        {
            get
            {
                InputY jDir = InputY.Default;

                if (_buttonBindingTable.TryGetValue(BindingId.TeleportPlus, out var buttonR) && _gamepad.GetButtonState(buttonR) == InputX.IsDown)
                    jDir = InputY.Up;
                else if (_buttonBindingTable.TryGetValue(BindingId.TeleportMinus, out var buttonL) && _gamepad.GetButtonState(buttonL) == InputX.IsDown)
                    jDir = InputY.Down;
                return jDir;

            }
        }

        private double _speedMulti = 0, _rotSpeedmulti = 0;

        public double Speedmulti => (_buttonBindingTable.TryGetValue(BindingId.Speedmulti, out var button) && _gamepad.GetButtonState(button) == InputX.IsHold) ? _speedMulti : 1;

        public double RotSpeedmulti => (_buttonBindingTable.TryGetValue(BindingId.RotSpeedMulti, out var button) && _gamepad.GetButtonState(button) == InputX.IsHold) ? _rotSpeedmulti : 1;

        public double ElevateUp => _axisBindingTable.TryGetValue(BindingId.ElevatePlus, out var axis) ? _gamepad.GetAxisValue(axis) : 0;

        public double ElevateDown => _axisBindingTable.TryGetValue(BindingId.ElevateMinus, out var axis) ? _gamepad.GetAxisValue(axis) : 0;

        public ActionDispatcher()
        {
            DaxsConfig config = DaxsConfig.Instance;

            _speedMulti = config.BindNumeric(BindingId.Speedmulti, v => _speedMulti = v);
            _rotSpeedmulti = config.BindNumeric(BindingId.RotSpeedMulti, v => _rotSpeedmulti = v);

            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (button == GamepadButton.Invalid || button == GamepadButton.Count)
                    continue;

                BindingId gAction = config.BindAction(button, v =>
                {
                    BindingId aEnum = Enum.Parse<BindingId>(v);

                    AddToButtonTable(button, aEnum);
                    ResetButtonBinding(aEnum, button);
                });

                AddToButtonTable(button, gAction);
            }

            foreach (GamepadAxis axis in Enum.GetValues<GamepadAxis>())
            {
                if (axis == GamepadAxis.Invalid || axis == GamepadAxis.Count)
                    continue;

                BindingId gAction = config.BindAction(axis, v =>
                {
                    BindingId aEnum = Enum.Parse<BindingId>(v);
                    AddToAxisTable(axis, aEnum);
                    ResetAxisBinding(aEnum, axis);
                });

                AddToAxisTable(axis, gAction);
            }
        }

        private void AddToButtonTable(GamepadButton button, BindingId gAction)
        {
            if (gAction == BindingId.Unset)
                _actionToButtonTable.Remove(button);
            else
                _actionToButtonTable[button] = gAction;
        }

        private void AddToAxisTable(GamepadAxis axis, BindingId gAction)
        {
            if (gAction == BindingId.Unset)
                _actionToAxisTable.Remove(axis);
            else
                _actionToAxisTable[axis] = gAction;
        }

        private void ResetButtonBinding(BindingId action, GamepadButton button)
        {
            foreach (var key in _buttonBindingTable.Where(kv => kv.Value.Equals(button)).Select(kv => kv.Key).ToList())
                _buttonBindingTable.Remove(key);

            _buttonBindingTable[action] = button;
        }

        private void ResetAxisBinding(BindingId action, GamepadAxis axis)
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
                BindingId actionType = kvPair.Value;

                if (_actionTable.TryGetValue(actionType, out IAction action) &&
                    _gamepad.GetButtonState(button) == action.Input &&
                    _gate.Allows(actionType, action))
                {
                    hasActions = true;
                    _actionQueue.Enqueue(action);
                    if (action is ActionBase bs && bs.WantsDeactivateCallback)
                        _frameActiveActions.Add(bs);
                }
            }

            foreach (var kvPair in _actionToAxisTable)
            {
                GamepadAxis axis = kvPair.Key;
                BindingId actionType = kvPair.Value;

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

                    if (action is ActionBase bs && bs.WantsDeactivateCallback)
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
            if (!HasActions)
                return;

            while (_actionQueue.TryDequeue(out var action))
            {
                action.Execute();
            }
        }

        /// <summary>
        /// Each cycle the gamepad state is updated
        /// </summary>
        /// <param name="gamepad"></param>
        internal void Update(Gamepad gamepad) => this._gamepad = gamepad;
    }
}