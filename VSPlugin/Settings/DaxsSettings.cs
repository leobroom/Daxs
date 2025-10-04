using System;
using System.Linq;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using System.Reflection;
using Rhino.UI.Controls;
using Rhino;

namespace Daxs
{
    public enum ActionEnum
    {
        Unset,
        LensPlus,
        LensMinus,
        LensDefault,
        TeleportPlus,
        TeleportMinus,
        Speedmulti,
        RotSpeedMulti,
        ElevatePlus, 
        ElevateMinus,
        SwitchMode,
        C1,
        C2,
        C3,
        C4,
        C5,
        C6
    }

    public class DaxsSettings : Dialog<bool>
    {
        private readonly Settings settings = Settings.Instance;
        private readonly Dictionary<string, NumericStepper> inputNumBoxes = new();
        private readonly Dictionary<string, CheckBox> inputBoolBoxes = new();
        readonly SortedDictionary<GButton, DropDown> inputActions = new SortedDictionary<GButton, DropDown>();

        private Button okButton;

        public DaxsSettings()
        {
            Title = "Daxs Gamepad Settings";
            ClientSize = new Size(500, 800);
            Resizable = false;

            CreateUi();

            // Handle key down for Escape
            this.KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Escape)
                {
                    this.Close(false);
                    e.Handled = true;
                }
            };             
        }

        void CreateUi()
        {
            var content = new TableLayout
            {
                Padding = new Padding(10, 10, 0, 10),
                Spacing = new Size(5, 10),
            };

            List<TableRow> rows = new();

            rows.Add(CreateControllerImage(ClientSize.Width - 50));

            string[] input =
{
                "YawSensitivity",
                "PitchSensitivity",
                "Deadzone",
                "MoveSpeed",
                "ElevateSpeed"
            };

            rows.AddRange(CreateLayout("Input Response", input));

            string[] hud =
            {
                "TextTime",
                "TextVisible",
            };

            rows.AddRange(CreateLayout("HUD", hud));

            string[] walk =
            {
                "EyeHeight",
                "MaximalJump",
            };

            rows.AddRange(CreateLayout("WalkMode", walk));

            rows.Add(new LabelSeparator { Text = "Input Layout" });

            rows.Add(AddButtonDropdowns());

            foreach (TableRow row in rows)
                content.Rows.Add(row);

            var scroll = new Scrollable{ Content = content, ExpandContentWidth = true, ExpandContentHeight = false };

            var page = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 10)
            };

            // scrollable region grows, shows vertical scrollbar when needed
            page.Rows.Add(new TableRow(scroll) { ScaleHeight = true });

            // footer stays visible
            page.Rows.Add(new TableRow(CreateDialogButtons()));

            Content = page;

            okButton.Focus();
        }

        void OnOk()
        {
            // Persist
            settings.SaveSettings();

            Result = true;
            Close();
        }

        void OnDefault()
        {
            foreach (NumericValue nv in settings.AllNumValues)
            {
                nv.Reset();
                if (inputNumBoxes.TryGetValue(nv.Name, out var box))
                    box.Value = nv.DisplayValue;
            }

            foreach (BooleanValue bv in settings.AllBoolValues)
            {
                bv.Reset();
                if (inputBoolBoxes.TryGetValue(bv.Name, out var box))
                    box.Checked = bv.Value;
            }

            foreach (TextValue tv in settings.AllTextValues) 
            {
                tv.Reset();

                string name = tv.Name.Replace("Button_", "");
                RhinoApp.WriteLine(name);

                if (Enum.TryParse(name, out GButton button)&& inputActions.TryGetValue(button, out var box))
                    box.SelectedKey = tv.Value;
            }  
        }

        private TableRow[] CreateLayout(string title, string[] names)
        {
            var inputLayout = new TableLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };

            foreach (var name in names)
                inputLayout.Rows.Add(GenerateUIControl(name));

            LabelSeparator seperator = new() { Text = title };

            TableRow[] result = { seperator, inputLayout };

            return result;
        }

        TableRow GenerateUIControl(string settingsName)
        {
            IValue val = settings[settingsName];
            Control control = null;

            if (val is NumericValue nv)
            {
                var box = new NumericStepper { Value = nv.DisplayValue };
                box.ValueChanged += (s, e) => nv.DisplayValue = box.Value;
                inputNumBoxes[nv.Name] = box;
                control = box;
            }
            else if (val is BooleanValue bv)
            {
                var box = new CheckBox { Checked = bv.Value };
                box.CheckedChanged += (s, e) => bv.Value = (bool)box.Checked;
                inputBoolBoxes[bv.Name] = box;
                control = box;
            }

            Label label = new() 
            { 
                Text = settingsName,
                Width =80
            };

            return new TableRow( new TableCell(label), new TableCell(control, scaleWidth: true));
        }

        readonly SortedDictionary<ActionEnum, string> actionTable = new SortedDictionary<ActionEnum, string>()
        {
            { ActionEnum.Unset, "Unset" },
            { ActionEnum.LensPlus, "Lens+" },
            { ActionEnum.LensMinus, "Lens-" },
            { ActionEnum.LensDefault, "LensDefault" },
            { ActionEnum.TeleportPlus, "Teleport+" },
            { ActionEnum.TeleportMinus, "Teleport-" },
            { ActionEnum.Speedmulti, "Speedmulti" },
            { ActionEnum.RotSpeedMulti, "RotSpeedMulti" },
            { ActionEnum.ElevatePlus, "ElevatePlus" },
            { ActionEnum.ElevateMinus, "ElevateMinus" },
            { ActionEnum.SwitchMode, "SwitchMode" },
            { ActionEnum.C1, "C1" },
            { ActionEnum.C2, "C2" },
            { ActionEnum.C3, "C3" },
            { ActionEnum.C4, "C4" },
            { ActionEnum.C5, "C5" },
            { ActionEnum.C6, "C6" }
        };

        private TableLayout AddButtonDropdowns()
        {
            var inputLayout = new TableLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };

            foreach (GButton button in Enum.GetValues<GButton>())
            {
                if (button == GButton.Unset)
                    continue;

                var tB = CreateActionDropdown(button);
                inputLayout.Rows.Add(tB);
            }

            SyncActionDropDowns();

            return inputLayout;
        }

        void SyncActionDropDowns()
        {
            if (_syncDropwon)
                return;

            _syncDropwon = true;

            // Master order to respect (SortedDictionary already provides a stable order)
            var masterOrder = actionTable.Keys.ToList();

            // For fast lookups
            var actionToText = actionTable;

            // Collect taken actions per other dropdowns
            var takenByDropdown = new Dictionary<DropDown, HashSet<ActionEnum>>();
            foreach (var kvA in inputActions)
            {
                DropDown dropdownA = kvA.Value;
                HashSet<ActionEnum> set = new();
                foreach (var kvB in inputActions)
                {
                    var dropdownB = kvB.Value;
                    if (dropdownB == dropdownA) // exclude self
                        continue; 

                    if (Enum.TryParse<ActionEnum>(dropdownB.SelectedKey, out var a2) && a2 != ActionEnum.Unset)
                        set.Add(a2);
                }
                takenByDropdown[dropdownA] = set;
            }

            // Rebuild each dropdown in the same order
            foreach (var kv in inputActions)
            {
                GButton button = kv.Key;
                DropDown dropdown = kv.Value;
                ActionEnum originalKey = Enum.TryParse(dropdown.SelectedKey, out ActionEnum myAction) ? myAction : ActionEnum.Unset;

                dropdown.DataStore = null;
                dropdown.Items.Clear();

                foreach (var action in masterOrder)
                {
                    if (action == ActionEnum.Unset)
                    {
                        dropdown.Items.Add(new ListItem { Key = action.ToString(), Text = actionToText[action] });
                        continue;
                    }

                    if (takenByDropdown[dropdown].Contains(action) && action != myAction) 
                        continue;

                    dropdown.Items.Add(new ListItem { Key = action.ToString(), Text = actionToText[action] });
                }

                dropdown.SelectedKey = myAction.ToString();

                if (settings["Button_" + button] is TextValue tVal)
                    tVal.Value = dropdown.SelectedKey;
            }

            _syncDropwon = false;
        }

        bool _syncDropwon = false;

        TableRow CreateActionDropdown(GButton button)
        {
            if (button == GButton.Unset)
                throw new Exception("Create DropDown failed! Button can't be unset");

            string buttonName = button.ToString();

            TextValue tVal = (TextValue)settings["Button_" + buttonName];

            string selectedKey = tVal.Value;

            if (!Enum.TryParse<ActionEnum>(selectedKey, out var selectedAction))
                throw new Exception("Faulty ActionEnum: " + selectedKey);

            Label label = new() { Text = buttonName, Width = 80 };
            DropDown dropdown = new() { };

            foreach (var key in actionTable.Keys)
            {
                string text = actionTable[key];
                dropdown.Items.Add(text, key.ToString());
            }

            dropdown.SelectedKey = selectedKey;
            dropdown.SelectedKeyChanged += (sender, args) =>
             {
                 if (_syncDropwon)
                     return;

                 SyncActionDropDowns();
             };

            inputActions.Add(button, dropdown);

            return new TableRow(label, dropdown);
        }


        TableLayout CreateDialogButtons()
        {

            okButton = new Button { Text = "CLOSE", Command = new Command((s, e) => OnOk()) };

            return new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 5),
                Rows =
                {
                    null,
                    new TableRow
                    (
                        null,
                        new TableCell(new Button { Text = "DEFAULT", Command = new Command((s, e) => OnDefault()) }, false),
                        new TableCell(okButton, false)
                    )
                }
            };
        }

        static ImageView CreateControllerImage(int targetWidth)
        {
            Bitmap bmp;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Daxs.Shared.DaxsGamepadLayout.png"))
            {
                bmp = new Bitmap(stream);
            }

            // preserve aspect ratio
            double aspect = (double)bmp.Width / bmp.Height;
            int w = Math.Max(1, targetWidth);
            int h = (int)Math.Round(w / aspect);

            return new ImageView
            {
                Image = bmp,
                Size = new Size(w, h) // ImageView scales image to control size
            };
        }
    }
}