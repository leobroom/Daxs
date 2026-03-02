using System;

namespace Daxs.Settings
{
    public class NumericValue : IValue
    {
        private double _value = 1;
        private double _displayValue = 1;
        private double _displayFactor = 1;
        private readonly double defaultValue = 1;

        public string Name { get; private set; } = "unset";

        public event EventHandler<double>? ValueChanged;


        public int DecimalPlaces { get; private set; } = 0;

        public double MinValue { get; private set; } = 0.001;
        public double MaxValue { get; private set; } = 100000;

        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                _displayValue = _value * _displayFactor;
                OnValueChanged(_value);
            }
        }

        public double DisplayValue
        {
            get => _displayValue;
            set
            {
                _displayValue = value;
                _value = (_displayFactor == 0) ? 0 : _displayValue / _displayFactor;
                OnValueChanged(_value);
            }
        }

        public double DisplayFactor
        {
            get => _displayFactor;
            set
            {
                if (value == 0)
                    return; // Avoid division by zero

                _displayFactor = value;
                _displayValue = _value * _displayFactor;
            }
        }

        public NumericValue(double value, double displayFactor, string name, double minValue, double maxValue, int decimalPlaces = 0)
        {
            Value = value;
            DisplayFactor = displayFactor;
            Name = name;
            defaultValue = value;
            DecimalPlaces = decimalPlaces;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public void Reset() => Value = defaultValue;

        protected virtual void OnValueChanged(double newValue) => ValueChanged?.Invoke(this, newValue);

        public override string ToString()=> $"{Name}: {Value}";
    }
}