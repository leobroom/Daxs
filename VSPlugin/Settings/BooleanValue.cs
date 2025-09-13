using System;

namespace Daxs
{
    public class BooleanValue : IValue
    {
        private bool? _value = false;
        private readonly bool _defaultValue = false;

        public string Name { get; private set; } = "unset";

        public event EventHandler<bool?> ValueChanged;

        public bool? Value
        {
            get => _value;
            set
            {
                if (_value == value) return; // avoid redundant events
                _value = value;
                OnValueChanged(_value);
            }
        }

        public BooleanValue(bool value, string name)
        {
            _value = value;
            _defaultValue = value;
            Name = name;
        }

        public void Toggle() => Value = !_value;

        public void Reset() => Value = _defaultValue;

        protected virtual void OnValueChanged(bool? newValue) => ValueChanged?.Invoke(this, newValue);

        public override string ToString() => $"{Name}: {Value}";
    }
}
