using System;

namespace Daxs
{
    public class TextValue : IValue
    {
        private string _value = "";
        private readonly string defaultValue = "";

        public string Name { get; private set; } = "unset";

        public event EventHandler<string>? ValueChanged;

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged(_value);
            }
        }

        public TextValue(string value, string name)
        {
            Value = value;
            Name = name;
            defaultValue = value;
        }

        public void Reset() => Value = defaultValue;

        protected virtual void OnValueChanged(string newValue) => ValueChanged?.Invoke(this, newValue);

        public override string ToString() => $"{Name}: {Value}";
    }
}