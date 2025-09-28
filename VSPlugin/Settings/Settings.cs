using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Daxs
{
    public class Settings
    {
        private static Settings instance = null;
        public static Settings Instance => instance ??= new Settings();

        private readonly Dictionary<string, NumericValue> numValues = new();
        private readonly Dictionary<string, BooleanValue> boolValues = new();
        //private readonly Dictionary<GButton, IAction> actionValues = new();

        //Serialization
        private static readonly JsonSerializerOptions jsonOpts = new()
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

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

            ////SpeedMulti
            //Add(GButton.L3, AProperty.Speedmulti);
            //Add(GButton.R3, AProperty.RotSpeedMulti);

            ////Elevator
            //Add(GButton.L2, AProperty.ElevateDown);
            //Add(GButton.R2, AProperty.ElevateUp);

            ////Teleport
            //Add(GButton.L1, AProperty.TeleportDown);
            //Add(GButton.R1, AProperty.TeleportUp);

            SaveSettings(); //For debug purposes

            LoadSettings();
        }

        private void Add(string name, double defaultValue, double displayFactor)
        { numValues[name] = new NumericValue(defaultValue, displayFactor, name); }

        private void Add(string name, bool defaultValue)
        { boolValues[name] = new BooleanValue(defaultValue, name); }

        //private void Add(GButton button,  AProperty actionName, params object[] args)
        //{ actionValues[button] = new ActionValue(button, actionName, args); }

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

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
                settings.SetDouble(nV.Name, nV.Value);

            foreach (BooleanValue bV in boolValues.Values) 
                settings.SetBool(bV.Name, bV.Value);

            //GetAllIActions

            Dictionary<GButton, Tuple<InputX, IAction>> actions = ActionManager.Instance.GetActions();
            List<IBase> baseActions = new List<IBase>();

            foreach (var item in actions.Values)
                baseActions.Add(item.Item2);

            SaveAllBindings(baseActions,  settings, "ActionBindingDtos");

            Dictionary<AProperty, IState> states = ActionManager.Instance.GetStates();
            List<IBase> baseStates = new List<IBase>();

            foreach (var item in states.Values)
                baseActions.Add(item);

            SaveAllBindings(baseStates,  settings, "StateBindingDtos");

            PlugIn.SavePluginSettings(id);
            RhinoApp.WriteLine($"settings saved.");
        }

        void SaveAllBindings(List<IBase> lst, PersistentSettings settings, string settingsName) 
        {
            List<ActionBindingDto> bindingDtos = new();

            foreach (var ibase in lst)
            {
                GButton button = ibase.Button;
                InputX input = ibase.Input;
                AProperty prop = ibase.Name;

                object[] args = ibase.GetArgs();

                ActionBindingDto dto = ToDto(button, prop, input, args);
                bindingDtos.Add(dto);
            }

            var json = JsonSerializer.Serialize(bindingDtos, jsonOpts);
            settings.SetString(settingsName, json);
        }
   
        public void LoadSettings()
        {
            Guid id = PlugIn.IdFromName("Daxs");

            PersistentSettings settings = PlugIn.GetPluginSettings(id, true);

            foreach (NumericValue nV in numValues.Values)
                nV.Value = settings.GetDouble(nV.Name, nV.Value);

            foreach (BooleanValue bV in boolValues.Values)
                bV.Value = settings.GetBool(bV.Name, bV.Value);

            // actions
            if (settings.TryGetString("ActionBindingDtos", out var json) && !string.IsNullOrWhiteSpace(json))
            {
                Dictionary<GButton, Tuple<InputX, IAction>> actions = new();

                try
                {
                    var list = JsonSerializer.Deserialize<List<ActionBindingDto>>(json, jsonOpts) ?? new();
                    foreach (var dto in list)
                    {
                        InputX input = dto.Input;
                        IAction action = FromDto(dto);
                        Tuple<InputX, IAction> tp = Tuple.Create(input, action);
                        actions.Add(dto.Button, tp);
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine("Failed to parse ActionBindingDtos: " + ex.Message);
                }

                if (actions.Count > 0)
                    ActionManager.Instance.SetActions(actions);
            } 

            RhinoApp.WriteLine($"settings loaded.");
        }


        //Serialisation
        private static ActionBindingDto ToDto(GButton button, AProperty property, InputX input, object [] args)
        {
            var dto = new ActionBindingDto
            {
                Button = button,
                Property = property,
                Input = input
            };

            foreach (var obj in args)
            {
                dto.Args.Add(obj switch
                {
                    double d => new ArgDto { Kind = "double", Value = d.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    bool b => new ArgDto { Kind = "bool", Value = b ? "true" : "false" },
                    string s => new ArgDto { Kind = "string", Value = s },
                    InputX x => new ArgDto { Kind = "enum:InputX", Value = x.ToString() },
                    InputY y => new ArgDto { Kind = "enum:InputY", Value = y.ToString() },
                    AProperty p => new ArgDto { Kind = "enum:AProperty", Value = p.ToString() },
                    GButton gb => new ArgDto { Kind = "enum:GButton", Value = gb.ToString() },
                    _ => new ArgDto { Kind = "string", Value = obj?.ToString() ?? "" } // fallback
                });
            }

            return dto;
        }

        private static object ParseArg(ArgDto a)
        {
            switch (a.Kind)
            {
                case "double":
                    if (double.TryParse(a.Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var d))
                        return d;
                    return 0.0;

                case "bool":
                    return string.Equals(a.Value, "true", StringComparison.OrdinalIgnoreCase);

                case "string":
                    return a.Value;

                case "enum:InputX":
                    return Enum.TryParse(typeof(InputX), a.Value, out var ex) ? ex : InputX.IsUnset;

                case "enum:InputY":
                    return Enum.TryParse(typeof(InputY), a.Value, out var ey) ? ey : InputY.Default;

                case "enum:AProperty":
                    return Enum.TryParse(typeof(AProperty), a.Value, out var ap) ? ap : AProperty.Unset;

                case "enum:GButton":
                    return Enum.TryParse(typeof(GButton), a.Value, out var gb) ? gb : GButton.Unset;

                default:
                    // unknown kind -> keep as string
                    return a.Value;
            }
        }

        private static IAction FromDto(ActionBindingDto dto)
        {
            var args = dto.Args.ConvertAll(ParseArg).ToArray();
            AProperty prop = dto.Property;

            switch (prop)
            {
                case AProperty.Speedmulti:
                    break;
                case AProperty.RotSpeedMulti:
                    break;
                case AProperty.ElevateUp:
                    break;
                case AProperty.ElevateDown:
                    break;
                case AProperty.TeleportUp:
                    break;
                case AProperty.TeleportDown:
                    break;
                case AProperty.DaxSettings:
                case AProperty.Custom:
                    return new RhinoCustomAction(args);
                case AProperty.Switch:
                    return new SwitchAction();
                case AProperty.Lens:
                    return new LensAction(args);

                default:
                    break;
            }

            throw new Exception("AProperty invalid :" + prop);
        }
    }
}