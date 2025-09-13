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

        private Settings()
        {
            //Gamepad
            Add("YawSensitivity", 0.0009, 100000);
            Add("PitchSensitivity", 0.0009, 100000);
            Add("Deadzone", 0.169, 1000);
            Add("MoveSpeed", 140.6, 0.1);
            Add("ElevateSpeed", 140.6, 0.1);

            //Text
            Add("TextTime", 2000, 1);
            Add("TextVisible", true);

            //Walking
            Add("EyeHeight", 1.7, 1);
            Add("MaximalJump", 1.5, 1);

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        { numValues[name] = new NumericValue(defaultValue, displayFactor, name); }

        private void Add(string name, bool defaultValue)
        { boolValues[name] = new BooleanValue(defaultValue, name); }

        public IValue this[string name]
        {
            get
            {
                if (numValues.TryGetValue(name, out var num))
                    return num;
                if (boolValues.TryGetValue(name, out var b))
                    return b;

                throw new KeyNotFoundException($"No setting with the name '{name}' was found.");
            }
        }


        public IEnumerable<NumericValue> AllNumValues => numValues.Values;

        public void SaveSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");
            //PlugInInfo info = PlugIn.GetPlugInInfo(id);

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
            {
                settings.SetDouble(nV.Name, nV.Value);
            }

            PlugIn.SavePluginSettings(id);

            RhinoApp.WriteLine($"settings saved.");
        }

        public void LoadSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");
            //PlugInInfo info = PlugIn.GetPlugInInfo(id);

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
                nV.Value = settings.GetDouble(nV.Name, nV.Value);

            Rhino.RhinoApp.WriteLine($"settings loaded.");
        }
    }
}
