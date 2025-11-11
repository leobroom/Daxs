using System;
using System.Linq;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using System.Reflection;
using static SDL3.SDL;
using Rhino;

namespace Daxs
{

    public class DaxsSettings : Dialog<bool>
    {
        private readonly Settings settings = Settings.Instance;

        private readonly Dictionary<string, Control> controlBoxes = new();

        private readonly SortedDictionary<GamepadButton, Tuple<Label, DropDown>> inputActions = new SortedDictionary<GamepadButton, Tuple<Label, DropDown>>();
        private readonly SortedDictionary<GAction, string> actionTable = new SortedDictionary<GAction, string>()
        {
            { GAction.Unset, "Unset" },
            { GAction.LensPlus, "Lens +" },
            { GAction.LensMinus, "Lens -" },
            { GAction.LensDefault, "Lens Default" },
            { GAction.TeleportPlus, "Teleport +" },
            { GAction.TeleportMinus, "Teleport -" },
            { GAction.Speedmulti, "Speed Multi" },
            { GAction.RotSpeedMulti, "Rotation Multi" },
            { GAction.ElevatePlus, "Elevate +" },
            { GAction.ElevateMinus, "Elevate -" },
            { GAction.SwitchMode, "Switch Mode" },
            { GAction.C1, "C1" },
            { GAction.C2, "C2" },
            { GAction.C3, "C3" },
            { GAction.C4, "C4" },
            { GAction.C5, "C5" },
            { GAction.C6, "C6" }
        };

        private Button okButton;

        private Label controllerInfoLabel = new();



        public DaxsSettings()
        {
            Gamepad.Created += (s, e) => { Application.Instance.AsyncInvoke(() => SetGamepadType(e.Gamepad)); };
            Gamepad.Destroyed += (s, e) => { Application.Instance.AsyncInvoke(() => SetGamepadType(null)); };

            Title = "Daxs Gamepad Settings";
            ClientSize = new Size(500, 700);
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
            // --- Start/Stop button ---
            var toggleSwitch = new ToggleSwitch
            {
                IsOn = ControllerManager.Instance.State == ControllerManager.Status.Started,
                Width = 65,
            };

            toggleSwitch.Toggled += (s, isOn) =>
            {
                ControllerManager.Instance.Toggle();
                LayoutManager.Instance.Set(Layout.Menu);
                toggleSwitch.IsOn = ControllerManager.Instance.State == ControllerManager.Status.Started;
            };

            // --- Controller info label ---
            controllerInfoLabel = new Label
            {
                TextAlignment = TextAlignment.Left,
                TextColor = Colors.Gray,
                Wrap = WrapMode.Word
            };

            // --- HEADER: top layout ---
            var headerLayout = new TableLayout
            {
                Padding = new Padding(5, 0, 5, 10),
                Spacing = new Size(8, 5),
                Rows =
                {
                    new TableRow(
      
                        new TableCell(toggleSwitch, false),
                        controllerInfoLabel
                           // new TableCell(null, true) // spacer to absorb width
                    ),
                }
            };


            // --- SETTINGS TAB CONTENT ---
            var content = new TableLayout
            {
                Padding = new Padding(10, 10, 0, 10),
                Spacing = new Size(5, 10)
            };

            List<TableRow> rows = new();
            rows.Add(CreateControllerImage(ClientSize.Width - 50));

            string[] input = { "YawSensitivity", "PitchSensitivity", "Deadzone", "MoveSpeed", "ElevateSpeed" };
            string[] hud = { "TextTime", "TextVisible" };
            string[] walk = { "EyeHeight", "MaximalJump" };
            string[] lens = { "LensStep", "LensDefault" };

            rows.Add(EtoFactory.CreateGroupExpander("Input Response", input, name => CreateControl(name), true));
            rows.Add(EtoFactory.CreateGroupExpander("HUD", hud, name => CreateControl(name), true));
            rows.Add(EtoFactory.CreateGroupExpander("WalkMode", walk, name => CreateControl(name), true));
            rows.Add(EtoFactory.CreateGroupExpander("Lens+-", lens, name => CreateControl(name), true));
            rows.Add(EtoFactory.CreateContentExpander("Input Layout", AddButtonDropdowns(), true));
            rows.Add(CreateCustom());

            foreach (TableRow row in rows)
                content.Rows.Add(row);

            var githubLink = new Label
            {
                Text = "© 2025 Leon Brohmann - Licensed under the MIT License - View on GitHub",
                Cursor = Cursors.Pointer,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Center
            };
            githubLink.MouseDown += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/leobroom/Daxs",
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            content.Rows.Add(new TableRow { ScaleHeight = true });
            content.Rows.Add(new TableRow(githubLink));

            var scroll = new Scrollable
            {
                Content = content,
                ExpandContentWidth = true,
                ExpandContentHeight = false,
                Border = BorderType.None
            };

            var settingsTab = new TabPage
            {
                Text = "⚙️ Settings",
                Content = scroll
            };

            // --- THEME COLOR TAB (NEW) ---
            var colorViewer = new ColorViewer();
            var themeScroll = new Scrollable
            {
                Content = colorViewer,
                ExpandContentWidth = true,
                ExpandContentHeight = false,
                Border = BorderType.None
            };
            var themeTab = new TabPage
            {
                Text = "🎨 Theme Colors",
                Content = themeScroll
            };

            // --- ABOUT TAB ---
            var aboutLayout = new DynamicLayout { Padding = 10, Spacing = new Size(10, 10) };
            aboutLayout.Add(new ImageView
            {
                Image = Bitmap.FromResource("Daxs.Shared.DaxsGamepadLayout.png"),
                Size = new Size(200, 120)
            });
            aboutLayout.Add(new Label
            {
                Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. " +
                       "Suspendisse lectus tortor, dignissim sit amet, adipiscing nec, ultricies sed, dolor.",
                Wrap = WrapMode.Word,
                TextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray
            });
            aboutLayout.Add(new Label
            {
                Text = "© 2025 Leon Brohmann – MIT License",
                TextAlignment = TextAlignment.Center,
                TextColor = Colors.SlateGray
            });

            var aboutTab = new TabPage
            {
                Text = "ℹ️ About",
                Content = aboutLayout
            };

            // --- MAIN TABS ---
            var tabs = new TabControl
            {
                Pages = { settingsTab, themeTab, aboutTab } // Inserted the new themeTab here
            };

            // --- BOTTOM BUTTONS ---
            var bottomButtons = EtoFactory.CreateButtonRow(new[]
            {
                ("DEFAULT", (Action)OnDefault),
                ("CLOSE", (Action)OnOk)
            });

            // --- FINAL PAGE LAYOUT ---
            var page = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(headerLayout),
                    new TableRow(tabs) { ScaleHeight = true },
                    new TableRow(bottomButtons)
                }
            };

            Content = page;

            SetGamepadType(ControllerManager.Instance.CurrentGamepad);
        }

        private void SetGamepadType(Gamepad gamepad)
        {
            controllerInfoLabel.Text = (gamepad != null) ? $"🎮 {gamepad.GpType}, {gamepad.Name} (VID:{gamepad.VendorID}, PID:{gamepad.ProductID})" : "⚠️ No Gamepad detected.";

            foreach (GamepadButton button in inputActions.Keys)
            {
                Label label = inputActions[button].Item1;
                bool hasButton = gamepad != null && gamepad.HasGamepadButton(button);

                label.Enabled = hasButton;
                label.Text = hasButton ? (gamepad.GetButtonLabel(button) is "Unknown" ? button.ToString() : gamepad.GetButtonLabel(button)) : button.ToString();


            }
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
                    if (Enum.TryParse(name, out GamepadButton button) && inputActions.TryGetValue(button, out var abox))
                        abox.Item2.SelectedKey = tv.Value;
                    else if (box is TextBox textBox)
                        textBox.Text = tv.Value;
                }
            }
        }

        TableRow CreateControl(string settingsName, string labelName = "")
        {
            IValue val = settings[settingsName];
            Control control = null;

            if (val is NumericValue nv)
            {
                var box = new NumericStepper
                {
                    Value = nv.DisplayValue,
                    DecimalPlaces = 2
                };
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
            okButton = new Button { Text = "CLOSE", Command = new Command((_, _) => OnOk()) };
            return EtoFactory.CreateButtonRow(new[]
            {
                ("DEFAULT", (Action)OnDefault),
                ("CLOSE", (Action)OnOk)
            });
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
            var innerLayout = EtoFactory.CreateLayout();

            for (int i = 1; i <= cCount; i++)
            {
                var layout = EtoFactory.CreateLayout();

                TableRow nameRow = CreateControl($"C{i}_Name", "Name");
                layout.Add(nameRow);
                layout.Add(CreateControl($"C{i}_Function", "RhinoScript"));
                layout.Add(CreateControl($"C{i}_SimulateKeys", "Simulate keys"));

                var textBox = (TextBox)nameRow.Cells[1].Control;
                var subExpander = CreateCustomExpander($"Custom {i}", $"C{i}", layout, textBox);

                innerLayout.Add(subExpander);
            }

            var mainExpander = EtoFactory.CreateExpander("Custom Commands", innerLayout, expanded: false);

            return new TableRow(mainExpander);
        }

        internal Expander CreateCustomExpander(string header, string tag, DynamicLayout content, TextBox boundTextBox)
        {
            Label label = new();
            string MakeHeader() => $"{header}{(string.IsNullOrEmpty(boundTextBox.Text) ? "" : " - ")}{boundTextBox.Text}";

            label.Text = MakeHeader();
            label.Tag = tag;
            SyncCustomNames();


            void SyncCustomNames()
            {
                string tag = (string)label.Tag;
                GAction action = Enum.Parse<GAction>(tag);
                actionTable[action] = label.Text;

                SyncActionDropDowns();
            }
            ;

            boundTextBox.TextChanged += (_, _) =>
            {

                label.Text = MakeHeader();
                SyncCustomNames();
            };

            return new Expander
            {
                Header = label,
                Content = content,
                Expanded = false,
                Padding = new Padding(0, 0, 0, 0)
            };
        }

        #region Dropdown

        private DynamicLayout AddButtonDropdowns()
        {
            var inputLayout = EtoFactory.CreateLayout();

            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                if (button == GamepadButton.Invalid || button == GamepadButton.Count)
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
                DropDown dropdownA = kvA.Value.Item2;
                HashSet<GAction> set = new();
                foreach (var kvB in inputActions)
                {
                    var dropdownB = kvB.Value.Item2;
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
                GamepadButton button = kv.Key;
                DropDown dropdown = kv.Value.Item2;
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

        TableRow CreateActionDropdown(GamepadButton button)
        {
            if (button == GamepadButton.Invalid || button == GamepadButton.Count)
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

            TableRow tr = EtoFactory.CreateControlRow(buttonName, dropdown);
            Label label = (Label)tr.Cells[0].Control;

            inputActions.Add(button, new Tuple<Label, DropDown>(label, dropdown));
            return tr;
        }

        #endregion
    }
}