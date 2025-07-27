// #! csharp
using System;
using System.IO;
using System.Collections.Generic;
using Rhino.PlugIns;

namespace Daxs
{
    public class Settings
    {
        private static Settings instance = null;
        public static Settings Instance => instance ??= new Settings();

        private readonly Dictionary<string, NumericValue> values = new();
        private readonly string settingsPath;

        private Settings()
        {
            settingsPath = Utils.GetFile("DaxsSettings.txt");

            Add("YawSensitivity", 0.009, 10000);
            Add("PitchSensitivity", 0.009, 10000);
            Add("Deadzone", 0.169, 1000);
            Add("MoveSpeed", 146, 1);

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        {values[name] = new NumericValue(defaultValue, displayFactor, name);}

        public NumericValue this[string name] => values[name];

        public IEnumerable<NumericValue> AllValues => values.Values;

        public void SaveSettings()
        {
            var lines = new List<string>();
            foreach (var val in values.Values)
                lines.Add($"{val.Name}={val.Value}");

            File.WriteAllLines(settingsPath, lines);
            Rhino.RhinoApp.WriteLine($"{settingsPath} saved.");
        }

        public void LoadSettings()
        {
            if (!File.Exists(settingsPath))
            {
                Rhino.RhinoApp.WriteLine($"{settingsPath} does not exist. Creating default...");
                SaveSettings();
                return;
            }

            foreach (var line in File.ReadAllLines(settingsPath))
            {
                var parts = line.Split('=');
                if (parts.Length == 2 && values.TryGetValue(parts[0], out var nv))
                {
                    if (double.TryParse(parts[1], out double parsed))
                        nv.Value = parsed;
                }
            }

            Rhino.RhinoApp.WriteLine($"{settingsPath} loaded.");
        }
    }
}