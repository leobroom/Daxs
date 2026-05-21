using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino;
using System.Collections;
using static SDL3.SDL;
using Daxs.Actions;
using Eto.Threading;
using System.Threading.Tasks;

namespace Daxs.Settings
{
    public partial class DaxsConfig : IEnumerable<IValue>
    {
        private static DaxsConfig instance = null;
        public static DaxsConfig Instance => instance ??= new DaxsConfig();

        private readonly Dictionary<string, IValue> iValues = new();

        private DaxsConfig()
        {
            //Autostart
            Add("AutoStart", "Start Daxs automatically when Rhino opens.", true);

            //Gamepad
            Add("YawSensitivity", "Horizontal look sensitivity.", 2.0, 100, 1, 1000);
            Add("PitchSensitivity", "Vertical look sensitivity.", 2.0, 100, 1, 1000);
            Add("InvertY-axis", "Invert vertical look direction.", false);

            Add("Deadzone", "Filters out minor stick drift.", 0.175, 1, 0, 1, 3);
            Add("FlySpeed", "Base movement speed in fly mode.", 25, 10, 1, 100000, 2);
            Add("WalkSpeed", "Base movement speed in walk mode.", 5, 10, 1, 100000, 2);
            Add("ElevateSpeed", "Vertical movement speed.", 25, 10, 1, 100000);
            Add("FlySpeedFactor", "Multiplier for fly mode speed.", 1, 1, 0.1, 10, 1);
            Add("WalkSpeedFactor", "Multiplier for walk mode speed.", 1, 1, 0.1, 10, 1);

            //Multiplicator
            Add(BindingId.Speedmulti, "Temporary movement speed boost.", 3, 1, 0, 10);
            Add(BindingId.RotSpeedMulti, "Temporary rotation speed boost.", 3, 1, 0, 10);

            //Text
            Add("TextTime", "Duration of on-screen messages in milliseconds.", 2000, 1, 0, 10000);
            Add("TextVisible", "Show on-screen messages.", true);

            //Walking
            Add("EyeHeight", "Camera height above the ground in walk mode.", 1.70, 1, 0, 10000, 2);
            Add("MaximalJump", "Maximum step height in walk mode.", 0.40, 1, 0, 10000, 2);

            //Lens
            Add("LensStep", "Camera lens length change per input step.", 1, 1, 1, 10, 2);
            Add("LensDefault", "Default camera lens length.", 35, 1, 1, 100, 2);

            //Developer
            Add("DeveloperMode", "Show developer/debug options.", false);

            foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
                Add(a, BindingId.Unset);

            //Gamepad
            foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
                Add(b, BindingId.Unset);

            Add(GamepadButton.Start, BindingId.Macro1); //DaxsSettings
            Add(GamepadButton.Back, BindingId.ChangeSpeed);

            //Add(GamepadButton.West, BindingId.NextViewport);

            Add(GamepadButton.North, BindingId.NextViewport);
            Add(GamepadButton.West, BindingId.Macro2); //ViewCaptureToFile
            Add(GamepadButton.East, BindingId.NextDisplaymode);
            Add(GamepadButton.South, BindingId.NextNamedView);

            Add(GamepadButton.RightShoulder, BindingId.TeleportPlus);
            Add(GamepadButton.LeftShoulder, BindingId.TeleportMinus);

            Add(GamepadAxis.RightTrigger, BindingId.ElevatePlus);
            Add(GamepadAxis.LeftTrigger, BindingId.ElevateMinus);

            Add(GamepadButton.LeftStick, BindingId.Speedmulti);
            Add(GamepadButton.RightStick, BindingId.RotSpeedMulti);

            Add(GamepadButton.DPadUp, BindingId.SwitchMode);
            Add(GamepadButton.DPadDown, BindingId.LensDefault);
            Add(GamepadButton.DPadLeft, BindingId.LensMinus);
            Add(GamepadButton.DPadRight, BindingId.LensPlus);

            //Macro1
            Add("Macro1_Name", "no tooltip", "DaxsSettings");
            Add("Macro1_Function", "no tooltip", "_Daxs_Settings");
            Add("Macro1_SimulateKeys", "no tooltip", true);

            //Macro2
            Add("Macro2_Name", "no tooltip", "ViewCaptureToFile");
            Add("Macro2_Function", "no tooltip", "_ViewCaptureToFile");
            Add("Macro2_SimulateKeys", "no tooltip", true);

            foreach (BindingId c in Enum.GetValues<BindingId>())
            {
                if (c < BindingId.Macro3 || c > BindingId.Macro6)
                    continue;

                Add($"{c}_Name", "no tooltip", "");
                Add($"{c}_Function", "no tooltip", "");
                Add($"{c}_SimulateKeys", "no tooltip", true);
                //Add($"{c}_Enabled", false);
            }

            LoadSettings();
        }
        private void Add(BindingId rnum, string toolTip, double defaultValue, double displayFactor, double minValue, double maxValue) => Add(rnum.ToString(), toolTip, defaultValue, displayFactor, minValue, maxValue);
        private void Add(string name, string toolTip, double defaultValue, double displayFactor, double minValue, double maxValue, int decimalPlaces = 0) => iValues[name] = new NumericValue(defaultValue, displayFactor, name, minValue, maxValue, toolTip, decimalPlaces);
        private void Add(string name, string toolTip, bool defaultValue) => iValues[name] = new BooleanValue(defaultValue, name, toolTip);
        private void Add(string name, string toolTip, string defaultValue) => iValues[name] = new TextValue(defaultValue, name, toolTip);
        private void Add(string name, string toolTip, BindingId defaultValue) => iValues[name] = new TextValue(defaultValue.ToString(), name, toolTip);
        private void Add(GamepadButton button, BindingId defaultValue) => Add(button.ToString(), "", defaultValue);
        private void Add(GamepadAxis axis, BindingId defaultValue) => Add(axis.ToString(), "", defaultValue);

        public IValue this[string name] => iValues.TryGetValue(name, out var v) ? v : throw new KeyNotFoundException($"No setting with the name '{name}' was found.");

        public IEnumerator<IValue> GetEnumerator() => iValues.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Bindings

        public double BindNumeric(BindingId key, Action<double> assign) => BindNumeric(key.ToString(), assign);

        public double BindNumeric(string key, Action<double> assign)
        {
            var nv = iValues[key] as NumericValue;
            nv.ValueChanged += (s, val) => assign(val);
            assign(nv.Value);
            return nv.Value;
        }
        public bool BindBoolean(string key, Action<bool> assign)
        {
            var bv = iValues[key] as BooleanValue;
            bv.ValueChanged += (s, val) => assign((bool)val);
            assign(bv.Value);
            return bv.Value;
        }

        public string BindText(string key, Action<string> assign)
        {
            var tv = iValues[key] as TextValue;
            tv.ValueChanged += (s, val) => assign(val);
            assign(tv.Value);
            return tv.Value;
        }

        public BindingId BindAction(GamepadButton key, Action<string> assign) => Enum.Parse<BindingId>(BindText(key.ToString(), assign));

        public BindingId BindAction(GamepadAxis key, Action<string> assign) => Enum.Parse<BindingId>(BindText(key.ToString(), assign));


        #endregion

        public void SaveSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (IValue iVal in iValues.Values)
            {
                if (iVal is NumericValue nV)
                    settings.SetDouble(nV.Name, nV.Value);
                else if (iVal is BooleanValue bV)
                    settings.SetBool(bV.Name, bV.Value);
                else if (iVal is TextValue tV)
                    settings.SetString(tV.Name, tV.Value);

                //settings.SetString($"{iVal.Name}_Tooltip", iVal.ToolTip);
            }

            PlugIn.SavePluginSettings(id);
            RhinoApp.WriteLine($"Daxs settings saved.");
        }

        public void LoadSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (IValue iVal in iValues.Values)
            {
                if (iVal is NumericValue nV)
                    nV.Value = settings.GetDouble(nV.Name, nV.Value);
                else if (iVal is BooleanValue bV)
                    bV.Value = settings.GetBool(bV.Name, bV.Value);
                else if (iVal is TextValue sV)
                    sV.Value = settings.GetString(sV.Name, sV.Value);

                //settings.GetString($"{iVal.Name}_Tooltip", iVal.ToolTip);
            }

            //RhinoApp.WriteLine($"settings loaded.");
        }
    }
}