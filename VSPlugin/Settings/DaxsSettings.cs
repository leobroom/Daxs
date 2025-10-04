using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI.Controls;
using System.Reflection;
using System.Linq;
using System;

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

            var inputLayout = new TableLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };

            // Example call (initially selects "Preset B")

            foreach (GButton button in Enum.GetValues<GButton>())
            {
                if (button == GButton.Unset)
                    continue;

                CreateActionDropdown(button);
            }

            //TableRow[] gpButtons =
            //{
            //    CreateDropdown("A",  GButton.A,      ActionOptions, "custom",  GetInitialKey(GButton.A)),
            //    CreateDropdown("B",  GButton.B,      ActionOptions, "custom",  GetInitialKey(GButton.B)),
            //    CreateDropdown("X",  GButton.X,      ActionOptions, "custom",  GetInitialKey(GButton.X)),
            //    CreateDropdown("Y",  GButton.Y,      ActionOptions, "custom",  GetInitialKey(GButton.Y)),
            //    CreateDropdown("Start", GButton.Start, ActionOptions, "custom", GetInitialKey(GButton.Start)),
            //    CreateDropdown("Back",  GButton.Back,  ActionOptions, "custom", GetInitialKey(GButton.Back)),
            //    CreateDropdown("L1",  GButton.L1,    ActionOptions, "custom",  GetInitialKey(GButton.L1)),
            //    CreateDropdown("L3",  GButton.L3,    ActionOptions, "custom",  GetInitialKey(GButton.L3)),
            //    CreateDropdown("R1",  GButton.R1,    ActionOptions, "custom",  GetInitialKey(GButton.R1)),
            //    CreateDropdown("R3",  GButton.R3,    ActionOptions, "custom",  GetInitialKey(GButton.R3)),
            //    CreateDropdown("DPad Up",    GButton.DPadUp,    ActionOptions, "custom", GetInitialKey(GButton.DPadUp)),
            //    CreateDropdown("DPad Down",  GButton.DPadDown,  ActionOptions, "custom", GetInitialKey(GButton.DPadDown)),
            //    CreateDropdown("DPad Left",  GButton.DPadLeft,  ActionOptions, "custom", GetInitialKey(GButton.DPadLeft)),
            //    CreateDropdown("DPad Right", GButton.DPadRight, ActionOptions, "custom", GetInitialKey(GButton.DPadRight)),
            //};


            //foreach (TableRow b in gpButtons)
            //    inputLayout.Rows.Add(b);

            //rows.Add(inputLayout);

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
            foreach (var nv in settings.AllNumValues)
            {
                nv.Reset();
                if (inputNumBoxes.TryGetValue(nv.Name, out var box))
                    box.Value = nv.DisplayValue;
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

        SortedDictionary<GButton, DropDown> actions = new SortedDictionary<GButton, DropDown>();

        SortedDictionary<ActionEnum, string> actionTable = new SortedDictionary<ActionEnum, string>()
        {
            { ActionEnum.Unset, "Unset" },
            { ActionEnum.LensPlus, "LensPlus" },
            { ActionEnum.LensMinus, "LensMinus" },
            { ActionEnum.LensDefault, "LensDefault" },
            { ActionEnum.TeleportPlus, "TeleportPlus" },
            { ActionEnum.TeleportMinus, "TeleportMinus" },
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

        void SyncActionDropDowns()
        {
            SortedDictionary<ActionEnum, string> options = new SortedDictionary<ActionEnum, string>(actionTable);

            //Removes doubles
            foreach (var action in actions) 
            {
                DropDown dd = action.Value;
                ActionEnum actionKey = Enum.Parse<ActionEnum>(dd.SelectedKey);
                if (actionKey == ActionEnum.Unset)
                    continue;

                options.Remove(actionKey);
            }

            //Set Options
            foreach (var action in actions)
            {
                DropDown dd = action.Value;
                dd.Items.Clear();

                foreach (var key in options.Keys)
                    dd.Items.Add(new ListItem { Key = key.ToString(), Text = options[key] });
            }
        }

        private void RemoveDoubleDropDownValues(DropDown dd)
        {
            string key = dd.SelectedKey;
            string unset = ActionEnum.Unset.ToString();
            if (key == unset)
                return;

            foreach (var action in actions)
            {
                DropDown ddCompare = action.Value;

                if (dd.Equals(ddCompare))
                    continue;

                if(dd.SelectedKey == ddCompare.SelectedKey)
                    ddCompare.SelectedKey= unset;
            }
        }

        TableRow CreateActionDropdown(GButton button)
        {
            if (button == GButton.Unset)
                throw new Exception("Create DropDown failed! Button can't be unset");

            string buttonName = button.ToString();

            TextValue tVal = (TextValue)settings["B_" + buttonName];

            string selectedValue = tVal.Value;

            if (!Enum.TryParse<ActionEnum>(selectedValue, out var selectedAction))
                throw new Exception("Faulty ActionEnum: " + selectedValue);

            // LABEL
            Label label = new() { Text = buttonName, Width = 80 };

            // DROPDOWN
            DropDown dropdown = new() {SelectedKey = selectedValue};

            dropdown.SelectedKeyChanged += (_, __) => 
            { 
                tVal.Value = dropdown.SelectedKey;
                RemoveDoubleDropDownValues(dropdown);
                SyncActionDropDowns();
            };

            actions.Add(button, dropdown);

            // Done
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