using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI.Controls;
using System.Reflection;
using System.Linq;
using System;
using System.Reflection.Metadata.Ecma335;

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


            var inputOptions = new (string Key, string Text)[]
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
                ("custom", "Custom…") // selecting this should enable the textbox
             };

            var inputLayout = new TableLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };

            // Example call (initially selects "Preset B")

            TableRow[] gpButtons =
          {
                CreateDropdown( "A",  inputOptions,  "custom",  "none"),
                CreateDropdown( "B",  inputOptions,  "custom",  "none"),
                CreateDropdown( "X",  inputOptions,  "custom",  "none"),
                CreateDropdown( "Y",  inputOptions,  "custom",  "none"),
                CreateDropdown( "Start",  inputOptions,  "custom",  "none"),
                CreateDropdown( "Back",  inputOptions,  "custom",  "none"),
                CreateDropdown( "L1",  inputOptions,  "custom",  "none"),
                CreateDropdown( "L3",  inputOptions,  "custom",  "none"),
                CreateDropdown( "R1",  inputOptions,  "custom",  "none"),
                CreateDropdown( "R3",  inputOptions,  "custom",  "none"),
                CreateDropdown( "DPad Up",  inputOptions,  "custom",  "none"),
                CreateDropdown( "DPad Down",  inputOptions,  "custom",  "none"),
                CreateDropdown( "DPad Left",  inputOptions,  "custom",  "none"),
                CreateDropdown( "DPad Right",  inputOptions,  "custom",  "none")
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
            Result = true;
            settings.SaveSettings();
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

        TableRow CreateDropdown(string labelText, (string Key, string Text)[] options,string activateKey = "custom", string? initialKey = null)
        {
            //LABEL
            Label label = new()
            {
                Text = labelText,
                Width = 80
            };

            //DOWPDOWN
            var dropdown = new DropDown();
            foreach (var (key, text) in options)
                dropdown.Items.Add(new ListItem { Key = key, Text = text });

            // Select initial item
            if (!string.IsNullOrEmpty(initialKey) && options.Any(o => o.Key == initialKey))
                dropdown.SelectedKey = initialKey;
            else if (dropdown.Items.Count > 0)
                dropdown.SelectedIndex = 0;

            // Value Changed
            //box.ValueChanged += (s, e) => nv.DisplayValue = box.Value;
            dropdown.SelectedValueChanged += (s, e) => { }; //continue here


            var customText = new TextBox
            {
                PlaceholderText = "Custom…",
                Width = 120
            };

            var cbox = new CheckBox { Checked = true };
            cbox.ToolTip = "Simulate Keyboard";

            void Sync()
            {
                bool isActive = dropdown.SelectedKey == activateKey;
                customText.Enabled = isActive;
                customText.Visible = isActive;     // hide when inactive (optional)

                cbox.Enabled = isActive;
                cbox.Visible = isActive;

                if (isActive) 
                    customText.Focus();
                else
                    customText.Text = string.Empty; // optional clear
            }

            dropdown.SelectedIndexChanged += (_, __) => Sync();

            Sync(); // set initial state without using protected members

            return new TableRow(label, dropdown, customText, cbox);
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