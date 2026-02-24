using Rhino;
using SDL3;
using System;
using System.Runtime.InteropServices;
using static SDL3.SDL;

namespace Daxs
{
    internal class CustomLayout : BaseLayout
    {
        public override Layout Name => Layout.Custom;

        private bool _speedAdjustActive;
        private BaseState _owner;

        private NumericValue _speedFactor = null;
        private double _angleDeg = -1;
        private double _lastAngle = -1;

        public CustomLayout() : base() 
        {
            _speedFactor = (NumericValue)settings["SpeedFactor"];
        }

        public void EnterSpeedAdjustMode(BaseState owner)
        {
            _owner = owner;

            _speedFactorVal = _speedFactor.Value;
            _speedAdjustActive = true;
            RhinoApp.WriteLine("_speedFactorVal: " + _speedFactorVal);
        }

        public void ExitSpeedAdjustMode(BaseState owner)
        {
            if (!ReferenceEquals(_owner, owner))
                return;

            _speedFactor.Value = _speedFactorVal;

            RhinoApp.WriteLine("_speedFactor.Value: " + _speedFactor.Value);
            _speedAdjustActive = false;
            _owner = null;
            _angleDeg = -1;
            _lastAngle = -1;
        }



        enum KnobState
        {
            NotInitialized = 0,
            Started = 1,
            Stopped = 2,
            MinHit = 3,
            MaxHit = 4,
        }

        KnobState _knobState = KnobState.NotInitialized;

        double clMin = 40;
        double clMax = 360 - 40;

        private double _speedFactorVal = -1;

        public override void HandleInput(Gamepad state)
        {
            if (!_speedAdjustActive)
                return;

            actionManager.QueueActions();

            var (x, y) = NormalizeStick(state.GetAxisValue(GamepadAxis.RightX), state.GetAxisValue(GamepadAxis.RightY));

            double length = Math.Sqrt(x * x + y * y);

            if (length > deadzone) // deadzone
            {
                double angleRad = Math.Atan2(x, -y);
                _angleDeg = angleRad * (180.0 / Math.PI);

                if (_angleDeg < 0)
                    _angleDeg += 360.0;
            }
            else
            {
                _knobState = KnobState.Stopped;
            }

            double MIN = 30;
            double MAX = 330;

            switch (_knobState)
            {
                case KnobState.NotInitialized:
                    _knobState = KnobState.Started;
                    ChangeAngle();
                    break;
                case KnobState.Stopped:
                default:
                    _knobState = KnobState.NotInitialized;
                    break;

                case KnobState.Started:
                    if (_lastAngle > MAX && _angleDeg < MIN)
                    {
                        _angleDeg = 360;
                        _knobState = KnobState.MaxHit;
                    }
                    else if (_lastAngle < MIN && _angleDeg > MAX)
                    {
                        _angleDeg = 0;
                        _knobState = KnobState.MinHit;
                    }

                    ChangeAngle();
                    break;
                case KnobState.MinHit:
                    if (_angleDeg < MIN)
                    {
                        ChangeAngle();
                        _knobState = KnobState.Started;
                    }
                    break;
                case KnobState.MaxHit:
                    if (_angleDeg > MAX)
                    {
                        ChangeAngle();
                        _knobState = KnobState.Started;
                    }
                    break;
            }

            _speedFactorVal = (_lastAngle ==-1) ? _speedFactorVal : NormalizeNumber(_lastAngle);

            OverlayRenderer.Instance.SetDonut("FLIGHT\nSPEED", _speedFactorVal, clMin, clMax, 200);

            if ((actionManager.HasActions) && sinceLastUi >= uiDt && !_uiUpdatePending)
            {
                _uiUpdatePending = true;

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    actionManager.ExecuteActionsOnMainThread();

                    _uiUpdatePending = false;
                }));
            }
        }

        double NormalizeNumber(double current) 
        {
            double clamped = Math.Clamp(current, clMin, clMax);

            double range = clMax - clMin;
            double normalized = clamped - clMin;

            double val = (normalized / range) * 10.0;

            return Math.Round(val, 1);
        }

        void ChangeAngle() 
        {
            int last = (int)Math.Floor(NormalizeNumber(_angleDeg));
            int current = (int)Math.Floor(NormalizeNumber(_lastAngle));

            _lastAngle = _angleDeg;
          
            if(current != last)
                GamepadRuntime.Instance.RumbleGamepad(0, 30000, 20);
        }
    }
}