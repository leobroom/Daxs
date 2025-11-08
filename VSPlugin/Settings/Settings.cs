using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino;
using System.Collections;
using static SDL3.SDL;

namespace Daxs
{
    public partial class Settings : IEnumerable<IValue>
    {
        private static Settings instance = null;
        public static Settings Instance => instance ??= new Settings();

        private readonly Dictionary<string, IValue> iValues = new();

        private Settings()
        {
            //Gamepad
            Add("YawSensitivity", 0.0009, 100000);
            Add("PitchSensitivity", 0.0009, 100000);
            Add("Deadzone", 0.169, 1000);
            Add("MoveSpeed", 140.6, 0.1);
            Add("ElevateSpeed", 140.6, 0.1);

            //Multiplicator
            Add(GAction.Speedmulti, 3, 1);
            Add(GAction.RotSpeedMulti, 3, 1);

            //Text
            Add("TextTime", 2000, 1);
            Add("TextVisible", true);

            //Walking
            Add("EyeHeight", 1.7, 1);
            Add("MaximalJump", 1.5, 1);

            //Lens
            Add("LensStep", 1, 1);
            Add("LensDefault", 35, 1);

            foreach (GamepadAxis a in Enum.GetValues<GamepadAxis>())
                Add(a, GAction.Unset);

            //Gamepad

            foreach (GamepadButton b in Enum.GetValues<GamepadButton>())
                Add(b, GAction.Unset);

            Add(GamepadButton.East, GAction.C1);
            Add(GamepadButton.Start, GAction.C2);

           
            Add(GamepadButton.LeftShoulder, GAction.TeleportPlus);
            Add(GamepadButton.RightShoulder, GAction.TeleportMinus);

            Add(GamepadAxis.LeftTrigger, GAction.ElevatePlus);
            Add(GamepadAxis.RightTrigger, GAction.ElevateMinus);

            Add(GamepadButton.LeftStick, GAction.Speedmulti);
            Add(GamepadButton.RightStick, GAction.RotSpeedMulti);

            Add(GamepadButton.DPadUp, GAction.SwitchMode);
            Add(GamepadButton.DPadDown, GAction.LensDefault);
            Add(GamepadButton.DPadLeft, GAction.LensMinus);
            Add(GamepadButton.DPadRight, GAction.LensPlus);

            //Custom1
            Add("C1_Name", "DaxsSettings");
            Add("C1_Function", "_Daxs_Settings");
            Add("C1_SimulateKeys", true);
            //Add("C1_Enabled", true);

            //Custom2
            Add("C2_Name", "ViewCaptureToFile");
            Add("C2_Function", "_ViewCaptureToFile");
            Add("C2_SimulateKeys", true);
           // Add("C2_Enabled", true);

            foreach (GAction c in Enum.GetValues<GAction>())
            {
                if (c < GAction.C3 || c > GAction.C6)
                    continue;

                Add($"{c}_Name", "");
                Add($"{c}_Function", "");
                Add($"{c}_SimulateKeys", true);
                //Add($"{c}_Enabled", false);
            }

            LoadSettings();
        }

        private void Add(GAction rnum, double defaultValue, double displayFactor) => Add(rnum.ToString(), defaultValue, displayFactor);
        private void Add(string name, double defaultValue, double displayFactor) => iValues[name] = new NumericValue(defaultValue, displayFactor, name); 
        private void Add(string name, bool defaultValue)=> iValues[name] = new BooleanValue(defaultValue, name); 
        private void Add(string name, string defaultValue) =>iValues[name] = new TextValue(defaultValue, name);
        private void Add(string name, GAction defaultValue)=>  iValues[name] = new TextValue(defaultValue.ToString(), name); 
        private void Add(GamepadButton button, GAction defaultValue) => Add(button.ToString(), defaultValue);

        private void Add(GamepadAxis axis, GAction defaultValue) => Add(axis.ToString(), defaultValue);

        public IValue this[string name] => iValues.TryGetValue(name, out var v)  ? v  : throw new KeyNotFoundException($"No setting with the name '{name}' was found.");

        public IEnumerator<IValue> GetEnumerator() => iValues.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
 

        #region Bindings

        public double BindNumeric(GAction key, Action<double> assign) => BindNumeric(key.ToString(), assign);


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

        public GAction BindAction(GamepadButton key, Action<string> assign)=> Enum.Parse<GAction>(BindText(key.ToString(), assign));

        public GAction BindAction(GamepadAxis key, Action<string> assign) => Enum.Parse<GAction>(BindText(key.ToString(), assign));


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
            RhinoApp.WriteLine($"settings saved.");
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

            RhinoApp.WriteLine($"settings loaded.");
        }
    }
}