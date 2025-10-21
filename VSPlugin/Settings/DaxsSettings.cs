using System;
using System.Linq;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using System.Reflection;
using Rhino.UI.Controls;

namespace Daxs
{
    public class DaxsSettings : Dialog<bool>
    {
        private readonly Settings settings = Settings.Instance;

        private readonly Dictionary<string, Control> controlBoxes = new();

        private readonly SortedDictionary<GButton, DropDown> inputActions = new SortedDictionary<GButton, DropDown>();
        private readonly SortedDictionary<GAction, string> actionTable = new SortedDictionary<GAction, string>()
        {
            { GAction.Unset, "Unset" },
            { GAction.LensPlus, "Lens+" },
            { GAction.LensMinus, "Lens-" },
            { GAction.LensDefault, "LensDefault" },
            { GAction.TeleportPlus, "Teleport+" },
            { GAction.TeleportMinus, "Teleport-" },
            { GAction.Speedmulti, "Speedmulti" },
            { GAction.RotSpeedMulti, "RotSpeedMulti" },
            { GAction.ElevatePlus, "ElevatePlus" },
            { GAction.ElevateMinus, "ElevateMinus" },
            { GAction.SwitchMode, "SwitchMode" },
            { GAction.C1, "C1" },
            { GAction.C2, "C2" },
            { GAction.C3, "C3" },
            { GAction.C4, "C4" },
            { GAction.C5, "C5" },
            { GAction.C6, "C6" }
        };

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

            rows.Add(CreateLayout("Input Response", input));

            string[] hud =
            {
                "TextTime",
                "TextVisible",
            };

            rows.Add(CreateLayout("HUD", hud));

            string[] walk =
            {
                "EyeHeight",
                "MaximalJump",
            };

            rows.Add(CreateLayout("WalkMode", walk));

            var inputLayoutExpander = new Expander
            {
                Header = new Label { Text = "Input Layout" },
                Content = AddButtonDropdowns(),
                Expanded = false
            };
            rows.Add(new TableRow(inputLayoutExpander));

            rows.Add(CreateCustom());

            foreach (TableRow row in rows)
                content.Rows.Add(row);

     
            //Scroll
            var scroll = new Scrollable{ Content = content, ExpandContentWidth = true, ExpandContentHeight = false };

            var page = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 10)
            };

            // scrollable region grows, shows vertical scrollbar when needed
            page.Rows.Add(new TableRow(scroll) { ScaleHeight = true });
            page.Rows.Add(CreateDialogButtons());

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
            foreach (IValue iv in settings)
            {
                iv.Reset();
                string name = iv.Name;

                controlBoxes.TryGetValue(name, out Control box);

                if (iv is NumericValue nv && box is NumericStepper stepper)
                    stepper.Value = nv.DisplayValue;
                else if (iv is BooleanValue bv && box is CheckBox checkB)
                    checkB.Checked = bv.Value;
                else if (iv is TextValue tv)
                {
                    if (Enum.TryParse(name, out GButton button) && inputActions.TryGetValue(button, out var abox))
                        abox.SelectedKey = tv.Value;
                    else if (box is TextBox textBox)
                        textBox.Text = tv.Value;
                }
            }
        }

        private TableRow CreateLayout(string title, string[] names)
        {
            var layout = EtoFactory.CreateLayout();

            foreach (var name in names)
                layout.Add(CreateControl(name));

            var expander = new Expander
            {
                Header = new Label { Text = title },
                Content = layout,
                Expanded = false, // collapsed by default
                Padding = new Padding(0, 0, 0, 0)
            };

            return new TableRow(expander);
        }



        TableRow CreateControl(string settingsName, string labelName ="")
        {
            IValue val = settings[settingsName];
            Control control = null;

            if (val is NumericValue nv)
            {
                var box = new NumericStepper { Value = nv.DisplayValue };
                box.ValueChanged += (s, e) => nv.DisplayValue = box.Value;
                controlBoxes[nv.Name] = box;
                control = box;
            }
            else if (val is BooleanValue bv)
            {
                var box = new CheckBox { Checked = bv.Value };
                box.CheckedChanged += (s, e) => bv.Value = (bool)box.Checked;
                controlBoxes[bv.Name] = box;
                control = box;
            }
            else if (val is TextValue tv)
            {
                var box = new TextBox { Text = tv.Value, PlaceholderText = "Enter Text..." };
                box.TextChanged += (s, e) => tv.Value = box.Text;
                controlBoxes[tv.Name] = box;
                control = box;
            }

            labelName = labelName == "" ? settingsName : labelName;

            return EtoFactory.CreateControlRow(labelName, control);
        }

        TableRow CreateDialogButtons()
        {
            okButton = new Button { Text = "CLOSE", Command = new Command((s, e) => OnOk()) };

            var layout = new TableLayout
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

            return new TableRow(layout);
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


        TableRow CreateCustom()
        {
            int cCount = 6;
            var innerLayout = new DynamicLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };

            for (int i = 1; i <= cCount; i++)
            {
                var layout = new DynamicLayout
                {
                    Padding = new Padding(10, 0, 0, 0),
                    Spacing = new Size(5, 5)
                };

                TableRow nRow = CreateControl($"C{i}_Name", "Name");
                layout.Add(nRow);
                layout.Add(CreateControl($"C{i}_Function", "RhinoScript"));
                layout.Add(CreateControl($"C{i}_SimulateKeys", "Simulate keys"));

                var tBox = (TextBox)nRow.Cells[1].Control;

                string CreateTxt() => $"Custom {i}{(string.IsNullOrEmpty(tBox.Text) ? "" : " - ")}{tBox.Text}";
                var headerLabel = new Label { Text = CreateTxt() };

                var subExpander = new Expander
                {
                    Header = headerLabel,
                    Content = layout,
                    Expanded = false,
                    Padding = new Padding(0, 0, 0, 0),
                };

                tBox.TextChanged += (sender, e) => headerLabel.Text = CreateTxt();

                innerLayout.Add(subExpander);
            }

            var mainExpander = new Expander
            {
                Header = new Label
                {
                    Text = "Custom Commands",
                    Font = SystemFonts.Bold(SystemFonts.Default().Size + 1)
                },
                Content = innerLayout,
                Expanded = false,
                Padding = new Padding(0, 0, 0, 0),
            };

            return new TableRow(mainExpander);
        }

        #region Dropdown

        private DynamicLayout AddButtonDropdowns()
        {
            var inputLayout = EtoFactory.CreateLayout();

            foreach (GButton button in Enum.GetValues<GButton>())
            {
                if (button == GButton.Unset)
                    continue;

                var tB = CreateActionDropdown(button);
                inputLayout.Add(tB);
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
            var actionToText = actionTable;

            // Collect taken actions per other dropdowns
            var takenByDropdown = new Dictionary<DropDown, HashSet<GAction>>();
            foreach (var kvA in inputActions)
            {
                DropDown dropdownA = kvA.Value;
                HashSet<GAction> set = new();
                foreach (var kvB in inputActions)
                {
                    var dropdownB = kvB.Value;
                    if (dropdownB == dropdownA) // exclude self
                        continue;

                    if (Enum.TryParse<GAction>(dropdownB.SelectedKey, out var a2) && a2 != GAction.Unset)
                        set.Add(a2);
                }
                takenByDropdown[dropdownA] = set;
            }

            // Rebuild each dropdown in the same order
            foreach (var kv in inputActions)
            {
                GButton button = kv.Key;
                DropDown dropdown = kv.Value;
                GAction originalKey = Enum.TryParse(dropdown.SelectedKey, out GAction myAction) ? myAction : GAction.Unset;

                dropdown.DataStore = null;
                dropdown.Items.Clear();

                foreach (var action in masterOrder)
                {
                    if (action == GAction.Unset)
                    {
                        dropdown.Items.Add(new ListItem { Key = action.ToString(), Text = actionToText[action] });
                        continue;
                    }

                    if (takenByDropdown[dropdown].Contains(action) && action != myAction)
                        continue;

                    dropdown.Items.Add(new ListItem { Key = action.ToString(), Text = actionToText[action] });
                }

                dropdown.SelectedKey = myAction.ToString();

                if (settings[button.ToString()] is TextValue tVal)
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

            TextValue tVal = (TextValue)settings[buttonName];

            string selectedKey = tVal.Value;

            if (!Enum.TryParse<GAction>(selectedKey, out var selectedAction))
                throw new Exception("Faulty ActionEnum: " + selectedKey);

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

            return EtoFactory.CreateControlRow(buttonName, dropdown);
        }

        #endregion
    }
}