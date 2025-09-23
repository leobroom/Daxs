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
        private readonly Dictionary<GButton, ActionValue> actionValues = new();

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

            //Input
            Add(GButton.Start, AProperty.DaxSettings, InputX.IsDown, true);
            Add(GButton.B,  AProperty.Custom, InputX.IsDown, true, "_ViewCaptureToFile");
            Add(GButton.DPadUp,  AProperty.Switch, InputX.IsDown);
            Add(GButton.DPadRight,  AProperty.Lens, InputX.IsDown, InputY.Up, 1.00);
            Add(GButton.DPadLeft,  AProperty.Lens, InputX.IsDown, InputY.Down, 1.00);
            Add(GButton.DPadDown,  AProperty.Lens, InputX.IsDown, InputY.Default, 35.00);

            //SpeedMulti
            Add(GButton.L3, AProperty.Speedmulti);
            Add(GButton.R3, AProperty.RotSpeedMulti);

            //Elevator
            Add(GButton.L2, AProperty.ElevateDown);
            Add(GButton.R2, AProperty.ElevateUp);

            //Teleport
            Add(GButton.L1, AProperty.TeleportDown);
            Add(GButton.R1, AProperty.TeleportUp);

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        { numValues[name] = new NumericValue(defaultValue, displayFactor, name); }

        private void Add(string name, bool defaultValue)
        { boolValues[name] = new BooleanValue(defaultValue, name); }

        private void Add(GButton button,  AProperty actionName, params object[] args)
        { actionValues[button] = new ActionValue(button, actionName, args); }

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

            foreach (BooleanValue bV in boolValues.Values) 
            {
                settings.SetBool(bV.Name, bV.Value);
            }

            foreach (ActionValue aV in actionValues.Values)
            { 
                List<string > stringLst = new List<string>();

                stringLst.Add(aV.ActionName.ToString());

                foreach (object obj in aV.Args) 
                    stringLst.Add(obj.ToString());

                settings.SetStringList(aV.Button.ToString(), stringLst.ToArray());
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

            BoolValues....
            ÂctionValues.

            RhinoApp.WriteLine($"settings loaded.");
        }
    }
}