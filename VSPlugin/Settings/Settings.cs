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

        private readonly Dictionary<string, NumericValue> values = new();

        private Settings()
        {
            //Gamepad
            Add("YawSensitivity", 0.0009, 100000);
            Add("PitchSensitivity", 0.0009, 100000);
            Add("Deadzone", 0.169, 1000);
            Add("MoveSpeed", 140.6, 0.1);
            Add("ElevateSpeed", 140.6, 0.1);

            //Text
            Add("TextTime", 2, 1);

            //Walking
            Add("EyeHeight", 1.7, 1);
            Add("MaximalJump", 1.5, 1);

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        { values[name] = new NumericValue(defaultValue, displayFactor, name); }

        public NumericValue this[string name] => values[name];

        public IEnumerable<NumericValue> AllValues => values.Values;

        public void SaveSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");
            //PlugInInfo info = PlugIn.GetPlugInInfo(id);

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in values.Values)
            {
                settings.SetDouble(nV.Name, nV.Value);
            }

            PlugIn.SavePluginSettings(id);

            Rhino.RhinoApp.WriteLine($"settings saved.");
        }

        public void LoadSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");
            //PlugInInfo info = PlugIn.GetPlugInInfo(id);

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in values.Values)
                nV.Value = settings.GetDouble(nV.Name, nV.Value);

            Rhino.RhinoApp.WriteLine($"settings loaded.");
        }
    }
}
