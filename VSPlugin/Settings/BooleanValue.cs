using System;

namespace Daxs.Settings
{
    public class BooleanValue : IValue
    {
        private bool _value = false;
        private readonly bool _defaultValue = false;

        public string Name { get; private set; } = "unset";

        public string ToolTip { get; private set; } = null;

        public event EventHandler<bool?> ValueChanged;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return; // avoid redundant events
                _value = value;
                OnValueChanged(_value);
            }
        }

        public object ObjectValue { get => Value; }

        public BooleanValue(bool value, string name, string toolTip)
        {
            _value = value;
            _defaultValue = value;
            Name = name;
            ToolTip = toolTip;
        }

        public void Toggle() => Value = !_value;

        public void Reset() => Value = _defaultValue;

        protected virtual void OnValueChanged(bool? newValue) => ValueChanged?.Invoke(this, newValue);

        public override string ToString() => $"{Name}: {Value}";
    }
}