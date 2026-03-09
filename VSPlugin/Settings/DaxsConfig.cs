using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino;
using System.Collections;
using static SDL3.SDL;
using Daxs.Actions;

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
            Add("AutoStart", true);

            //Gamepad
            Add("YawSensitivity", 1, 100,1,1000);
            Add("PitchSensitivity", 1, 100,1, 1000);
            Add("Deadzone", 0.175, 1, 0, 1,3);
            Add("MoveSpeed", 150, 1,0.001, 100000,2);
            Add("ElevateSpeed", 150, 1, 0.001, 100000, 2);
            Add("SpeedFactor", 1, 1,0.1,10, 1);

            //Multiplicator
            Add(BindingId.Speedmulti, 3, 1,0,10);
            Add(BindingId.RotSpeedMulti, 3, 1, 0, 10);

            //Text
            Add("TextTime", 2000, 1,0,10000);
            Add("TextVisible", true);

            //Walking
            Add("EyeHeight", 1.70, 1,0,10000,2);
            Add("MaximalJump", 0.40, 1, 0, 10000, 2);

            //Lens
            Add("LensStep", 1, 1,1,10,2);
            Add("LensDefault", 35, 1,1,100,2);

            foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
                Add(a, BindingId.Unset);

            //Gamepad
            foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
                Add(b, BindingId.Unset);

            Add(GamepadButton.Start, BindingId.Macro1);

            Add(GamepadButton.North, BindingId.NextView);
            Add(GamepadButton.West, BindingId.NextViewport);
            Add(GamepadButton.East, BindingId.NextDisplaymode);
            Add(GamepadButton.South, BindingId.NextNamedView);

            Add(GamepadButton.LeftShoulder, BindingId.TeleportPlus);
            Add(GamepadButton.RightShoulder, BindingId.TeleportMinus);

            Add(GamepadAxis.LeftTrigger, BindingId.ElevatePlus);
            Add(GamepadAxis.RightTrigger, BindingId.ElevateMinus);

            Add(GamepadButton.LeftStick, BindingId.Speedmulti);
            Add(GamepadButton.RightStick, BindingId.RotSpeedMulti);

            Add(GamepadButton.DPadUp, BindingId.SwitchMode);
            Add(GamepadButton.DPadDown, BindingId.LensDefault);
            Add(GamepadButton.DPadLeft, BindingId.LensMinus);
            Add(GamepadButton.DPadRight, BindingId.LensPlus);

            //Custom1
            Add("Macro1_Name", "DaxsSettings");
            Add("Macro1_Function", "_Daxs_Settings");
            Add("Macro1_SimulateKeys", true);

            //Custom2
            Add("Macro2_Name", "ViewCaptureToFile");
            Add("Macro2_Function", "_ViewCaptureToFile");
            Add("Macro2_SimulateKeys", true);

            foreach (BindingId c in Enum.GetValues<BindingId>())
            {
                if (c < BindingId.Macro3 || c > BindingId.Macro6)
                    continue;

                Add($"{c}_Name", "");
                Add($"{c}_Function", "");
                Add($"{c}_SimulateKeys", true);
                //Add($"{c}_Enabled", false);
            }

            LoadSettings();
        }
        private void Add(BindingId rnum, double defaultValue, double displayFactor, double minValue, double maxValue) => Add(rnum.ToString(), defaultValue, displayFactor,  minValue,  maxValue);
        private void Add(string name, double defaultValue, double displayFactor, double minValue, double maxValue, int decimalPlaces = 0) => iValues[name] = new NumericValue(defaultValue, displayFactor, name,  minValue,  maxValue, decimalPlaces); 
        private void Add(string name, bool defaultValue)=> iValues[name] = new BooleanValue(defaultValue, name); 
        private void Add(string name, string defaultValue) =>iValues[name] = new TextValue(defaultValue, name);
        private void Add(string name, BindingId defaultValue)=>  iValues[name] = new TextValue(defaultValue.ToString(), name); 
        private void Add(GamepadButton button, BindingId defaultValue) => Add(button.ToString(), defaultValue);
        private void Add(GamepadAxis axis, BindingId defaultValue) => Add(axis.ToString(), defaultValue);

        public IValue this[string name] => iValues.TryGetValue(name, out var v)  ? v  : throw new KeyNotFoundException($"No setting with the name '{name}' was found.");

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

        public BindingId BindAction(GamepadButton key, Action<string> assign)=> Enum.Parse<BindingId>(BindText(key.ToString(), assign));

        public BindingId BindAction(GamepadAxis key, Action<string> assign) => Enum.Parse<BindingId>(BindText(key.ToString(), assign));


        #endregion

        public void SaveSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (IValue iVal in iValues.Values)
            {
                if(iVal is NumericValue nV)
                    settings.SetDouble(nV.Name, nV.Value);
                else if(iVal is BooleanValue bV)
                    settings.SetBool(bV.Name, bV.Value);
                else if (iVal is TextValue tV)
                    settings.SetString(tV.Name, tV.Value);
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
            }

            //RhinoApp.WriteLine($"settings loaded.");
        }
    }
}