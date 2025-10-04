using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino;

namespace Daxs
{
    public class Settings
    {
        private static Settings instance = null;
        public static Settings Instance => instance ??= new Settings();

        private readonly Dictionary<string, NumericValue> numValues = new();
        private readonly Dictionary<string, BooleanValue> boolValues = new();
        private readonly Dictionary<string, TextValue> textValues = new();

        private Settings()
        {
            //Gamepad
            Add("YawSensitivity", 0.0009, 100000);
            Add("PitchSensitivity", 0.0009, 100000);
            Add("Deadzone", 0.169, 1000);
            Add("MoveSpeed", 140.6, 0.1);
            Add("ElevateSpeed", 140.6, 0.1);

            //Multiplicator
            Add("SpeedMultiplicator", 3, 1);
            Add("RotationMultiplicator", 3, 1);

            //Text
            Add("TextTime", 2000, 1);
            Add("TextVisible", true);

            //Walking
            Add("EyeHeight", 1.7, 1);
            Add("MaximalJump", 1.5, 1);

            //Lens
            Add("LensStep", 1, 1);
            Add("LensDefault", 35, 1);

            //Gamepad
            Add("Button_A", "unset");
            Add("Button_B", "C1");
            Add("Button_X", "unset");
            Add("Button_Y", "unset");

            Add("Button_Start", "C2");
            Add("Button_Back", "unset");

            Add("Button_L1", "TeleportDown");
            Add("Button_R1", "TeleportUp");

            Add("Button_L2", "ElevateDown");
            Add("Button_R2", "ElevateUp");

            Add("Button_L3", "Speedmulti");
            Add("Button_R3", "RotSpeedMulti");

            Add("Button_DPadUp", "SwitchMode");
            Add("Button_DPadDown", "LensDefault");
            Add("Button_DPadLeft", "Lens-");
            Add("Button_DPadRight", "Lens+");

            //Custom1
            Add("C1_Name", "DaxsSettings");
            Add("C1_Function", "_Daxs_Settings");
            Add("C1_SimulateKeys", true);
            Add("C1_Enabled", true);

            //Custom2
            Add("C2_Name", "unset");
            Add("C2_Function", "");
            Add("C2_SimulateKeys", true);
            Add("C2_Enabled", true);

            //Custom3
            Add("C3_Name", "unset");
            Add("C3_Function", "");
            Add("C3_SimulateKeys", true);
            Add("C3_Enabled", false);

            //Custom4
            Add("C4_Name", "unset");
            Add("C4_Function", "");
            Add("C4_SimulateKeys", true);
            Add("C4_Enabled", false);

            //Custom5
            Add("C5_Name", "unset");
            Add("C5_Function", "");
            Add("C5_SimulateKeys", true);
            Add("C5_Enabled", false);

            //Custom6
            Add("C6_Name", "unset");
            Add("C6_Function", "");
            Add("C6_SimulateKeys", true);
            Add("C6_Enabled", false);

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        { numValues[name] = new NumericValue(defaultValue, displayFactor, name); }

        private void Add(string name, bool defaultValue)
        { boolValues[name] = new BooleanValue(defaultValue, name); }

        private void Add(string name, string defaultValue) 
        {textValues[name] = new TextValue(defaultValue, name);}

        public IValue this[string name]
        {
            get
            {
                if (numValues.TryGetValue(name, out var num))
                    return num;
                if (boolValues.TryGetValue(name, out var b))
                    return b;
                if (textValues.TryGetValue(name, out var s))
                    return s;

                throw new KeyNotFoundException($"No setting with the name '{name}' was found.");
            }
        }

        public IEnumerable<NumericValue> AllNumValues => numValues.Values;

        public void SaveSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
                settings.SetDouble(nV.Name, nV.Value);

            foreach (BooleanValue bV in boolValues.Values) 
                settings.SetBool(bV.Name, bV.Value);

            foreach (TextValue nV in textValues.Values)
                settings.SetString(nV.Name, nV.Value);

            PlugIn.SavePluginSettings(id);
            RhinoApp.WriteLine($"settings saved.");
        }

        public void LoadSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
                nV.Value = settings.GetDouble(nV.Name, nV.Value);

            foreach (BooleanValue bV in boolValues.Values)
                bV.Value = settings.GetBool(bV.Name, bV.Value);

            foreach (TextValue sV in textValues.Values)
                sV.Value = settings.GetString(sV.Name, sV.Value);

            RhinoApp.WriteLine($"settings loaded.");
        }
    }
}