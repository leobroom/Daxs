using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI.Controls;
using System.Reflection;
using System.Linq;
using System;

namespace Daxs
{
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


        private string GetInitialKey(GButton button)
        {
            // Prefer actions; fall back to states
            var allActs = ActionManager.Instance.GetActions();
            var matchAct = allActs.FirstOrDefault(a => a.Button == button);

            if (matchAct != null)
            {
                switch (matchAct.Name)
                {
                    case AProperty.Lens:
                        var args = matchAct.GetArgs(); // (InputY dir, double step)
                        if (args.Length > 0 && args[0] is InputY dir)
                        {
                            return dir switch
                            {
                                InputY.Up => "lensPlus",
                                InputY.Down => "lensMinus",
                                _ => "lensDefault"
                            };
                        }
                        return "lensDefault";

                    case AProperty.Switch:
                        return "swichMode";

                    case AProperty.Custom:
                        // args: (string command, bool simulateKeyboard)
                        var a = matchAct.GetArgs();
                        if (a.Length >= 1 && a[0] is string cmd)
                        {
                            // populate custom widgets after the row exists (in CreateDropdown we sync)
                            return "custom";
                        }
                        return "custom";

                    case AProperty.DaxSettings:
                        return "daxsSettings";

                    default:
                        // View capture is a custom action we recognize from defaults
                        var ga = matchAct.GetArgs();
                        if (ga.Length >= 1 && ga[0] is string cmd2 && cmd2.Equals("_ViewCaptureToFile", StringComparison.OrdinalIgnoreCase))
                            return "viewCaptureToFile";
                        break;
                }
            }

            // Look at states (teleports etc.)
            var allStates = ActionManager.Instance.GetStates();
            var tpUp = allStates.FirstOrDefault(s => s.Button == button && s.Name == AProperty.TeleportUp);
            if (tpUp != null) return "teleportUp";

            var tpDown = allStates.FirstOrDefault(s => s.Button == button && s.Name == AProperty.TeleportDown);
            if (tpDown != null) return "teleportDown";

            return "none";
        }


        private readonly List<BindingRow> bindingRows = new();

        private record BindingRow(
            GButton Button,
            DropDown Dropdown,
            TextBox CustomText,
            CheckBox SimulateCheck
        );

        // Key => (kind, detail)
        // kind: "none", "lens", "custom", "switch", "cmd", "state"
        private static readonly (string Key, string Text)[] ActionOptions =
        {
            ("none", "None"),
            ("lensPlus", "Lens +"),
            ("lensMinus", "Lens -"),
            ("lensDefault", "Lens Default"),
            ("viewCaptureToFile", "ViewCaptureToFile"),
            ("swichMode", "Switch Gamepad Mode"),
            ("daxsSettings", "Daxs Settings"),
            ("teleportUp", "Teleport Up"),
            ("teleportDown", "Teleport Down"),
            ("custom", "Custom…")
        };


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

            TableRow[] gpButtons =
            {
                CreateDropdown("A",  GButton.A,      ActionOptions, "custom",  GetInitialKey(GButton.A)),
                CreateDropdown("B",  GButton.B,      ActionOptions, "custom",  GetInitialKey(GButton.B)),
                CreateDropdown("X",  GButton.X,      ActionOptions, "custom",  GetInitialKey(GButton.X)),
                CreateDropdown("Y",  GButton.Y,      ActionOptions, "custom",  GetInitialKey(GButton.Y)),
                CreateDropdown("Start", GButton.Start, ActionOptions, "custom", GetInitialKey(GButton.Start)),
                CreateDropdown("Back",  GButton.Back,  ActionOptions, "custom", GetInitialKey(GButton.Back)),
                CreateDropdown("L1",  GButton.L1,    ActionOptions, "custom",  GetInitialKey(GButton.L1)),
                CreateDropdown("L3",  GButton.L3,    ActionOptions, "custom",  GetInitialKey(GButton.L3)),
                CreateDropdown("R1",  GButton.R1,    ActionOptions, "custom",  GetInitialKey(GButton.R1)),
                CreateDropdown("R3",  GButton.R3,    ActionOptions, "custom",  GetInitialKey(GButton.R3)),
                CreateDropdown("DPad Up",    GButton.DPadUp,    ActionOptions, "custom", GetInitialKey(GButton.DPadUp)),
                CreateDropdown("DPad Down",  GButton.DPadDown,  ActionOptions, "custom", GetInitialKey(GButton.DPadDown)),
                CreateDropdown("DPad Left",  GButton.DPadLeft,  ActionOptions, "custom", GetInitialKey(GButton.DPadLeft)),
                CreateDropdown("DPad Right", GButton.DPadRight, ActionOptions, "custom", GetInitialKey(GButton.DPadRight)),
            };


            foreach (TableRow b in gpButtons)
                inputLayout.Rows.Add(b);

            rows.Add(inputLayout);

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
            // Build fresh maps based on UI
            var newActions = new Dictionary<GButton, IAction>();
            var newStates = new Dictionary<AProperty, IState>();

            foreach (var row in bindingRows)
            {
                var key = row.Dropdown.SelectedKey;

                switch (key)
                {
                    case "none":
                        break;

                    case "lensPlus":
                        newActions[row.Button] = new LensAction(row.Button, InputX.IsDown, InputY.Up, 1);
                        break;

                    case "lensMinus":
                        newActions[row.Button] = new LensAction(row.Button, InputX.IsDown, InputY.Down, 1);
                        break;

                    case "lensDefault":
                        newActions[row.Button] = new LensAction(row.Button, InputX.IsDown, InputY.Default, 35);
                        break;

                    case "viewCaptureToFile":
                        newActions[row.Button] = new RhinoCustomAction(row.Button, InputX.IsDown, "_ViewCaptureToFile", true);
                        break;

                    case "swichMode":
                        newActions[row.Button] = new SwitchAction(row.Button, InputX.IsDown);
                        break;

                    case "daxsSettings":
                        newActions[row.Button] = new RhinoCustomAction(row.Button, InputX.IsDown, "_Daxs_Settings", true);
                        break;

                    case "teleportUp":
                        newStates[AProperty.TeleportUp] = new State(row.Button, InputX.IsDown, AProperty.TeleportUp, "Teleport Up", 1.00);
                        break;

                    case "teleportDown":
                        newStates[AProperty.TeleportDown] = new State(row.Button, InputX.IsDown, AProperty.TeleportDown, "Teleport Down", 1.00);
                        break;

                    case "custom":
                        {
                            var cmd = row.CustomText.Text?.Trim();
                            var sim = row.SimulateCheck.Checked == true;
                            if (!string.IsNullOrWhiteSpace(cmd))
                                newActions[row.Button] = new RhinoCustomAction(row.Button, InputX.IsDown, cmd, sim);
                        }
                        break;
                }
            }

            // Apply to ActionManager
            ActionManager.Instance.SetActions(newActions);
            ActionManager.Instance.SetStates(newStates);

            // Persist
            settings.SaveSettings();

            Result = true;
            Close();
        }

        void OnApplyDefaultInputLayout()
        {
            // 1) Rebuild defaults in ActionManager
            ActionManager.Instance.ApplyDefaultBindings();

            // 2) Persist to Rhino settings
            settings.SaveSettings();

            // 3) Refresh all dropdowns + custom fields so the UI reflects the active defaults
            foreach (var row in bindingRows)
            {
                // Recompute which option should be selected for this button
                var key = GetInitialKey(row.Button);

                // Set dropdown and re-prefill the custom widgets if needed
                if (!string.IsNullOrEmpty(key))
                    row.Dropdown.SelectedKey = key;

                // Ensure visibility state is synced when switching to/from "custom"
                // (re-run the visibility logic that you already have in CreateDropdown)
                //row.Dropdown.SelectedIndexChanged(EventArgs.Empty);

                PrefillCustom(row.Button, row.Dropdown, row.CustomText, row.SimulateCheck);
            }
        }

        void OnDefault()
        {
            foreach (var nv in settings.AllNumValues)
            {
                nv.Reset();
                if (inputNumBoxes.TryGetValue(nv.Name, out var box))
                    box.Value = nv.DisplayValue;
            }

            OnApplyDefaultInputLayout();
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

        TableRow CreateDropdown(string labelText, GButton button, (string Key, string Text)[] options, string activateKey = "custom", string? initialKey = null)
        {
            // LABEL
            Label label = new() { Text = labelText, Width = 80 };

            // DROPDOWN
            var dropdown = new DropDown();
            foreach (var (key, text) in options)
                dropdown.Items.Add(new ListItem { Key = key, Text = text });

            // Select initial item
            if (!string.IsNullOrEmpty(initialKey) && options.Any(o => o.Key == initialKey))
                dropdown.SelectedKey = initialKey;
            else if (dropdown.Items.Count > 0)
                dropdown.SelectedIndex = 0;

            // WIDGETS for "custom"
            var customText = new TextBox { PlaceholderText = "Custom…", Width = 120 };
            var cbox = new CheckBox { Checked = true, ToolTip = "Simulate Keyboard" };

            // Sync visibility
            void Sync()
            {
                bool isActive = dropdown.SelectedKey == activateKey;
                customText.Enabled = isActive;
                customText.Visible = isActive;
                cbox.Enabled = isActive;
                cbox.Visible = isActive;
            }
            dropdown.SelectedIndexChanged += (_, __) => Sync();
            Sync();

            // If current binding is custom or recognizable commands, prefill
            PrefillCustom(button, dropdown, customText, cbox);

            // Remember row for saving
            bindingRows.Add(new BindingRow(button, dropdown, customText, cbox));

            // Done
            return new TableRow(label, dropdown, customText, cbox);

        }

        private void PrefillCustom(GButton button, DropDown dd, TextBox tb, CheckBox ck)
        {
            if (dd.SelectedKey != "custom") return;

            var act = ActionManager.Instance.GetActions().FirstOrDefault(a => a.Button == button);
            if (act != null && act.Name == AProperty.Custom)
            {
                var args = act.GetArgs(); // (string command, bool simulateKeyboard)
                if (args.Length >= 1 && args[0] is string cmd) 
                    tb.Text = cmd;
                if (args.Length >= 2 && args[1] is bool sim) 
                    ck.Checked = sim;
            }
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