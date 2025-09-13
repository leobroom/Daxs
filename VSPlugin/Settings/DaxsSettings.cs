using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino.UI.Controls;
using System.Reflection;
using System.Linq;

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
            ClientSize = new Size(400, 700);
            Resizable = false;

            var content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 10),
            };


            
            ImageView cImage = CreateControllerImage();
            TableLayout dialogButtons = CreateDialogButtons();

            List<TableRow> rows = new();

            string[] input =
            {
                "YawSensitivity",
                "PitchSensitivity",
                "Deadzone",
                "MoveSpeed",
                "ElevateSpeed"
            };

            rows.Add(new LabelSeparator { Text = "Gamepad Layout" });
            rows.Add(cImage);
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



            rows.Add(new LabelSeparator { Text = "Test" });

            // Dummy options to pass into your method
            var options = new (string Key, string Text)[]
            {
    ("none",   "None"),
    ("presetA","Preset A"),
    ("presetB","Preset B"),
    ("custom", "Custom…") // selecting this should enable the textbox
            };

            // Example call (initially selects "Preset B")
            var special = CreateRow_LabelDrop_CustomText(
                labelText: "Mode",
                options: options,
                activateKey: "custom",
                initialKey: "presetB"
            );

            rows.Add(special);

            rows.Add(dialogButtons);
         
            foreach (TableRow row in rows)
                content.Rows.Add(row);

            Content = new Scrollable
            {
                Content = content,
                ExpandContentWidth = true,   // fill width
                ExpandContentHeight = false  // natural height -> vertical scroll
            };

            // Handle key down for Escape
            this.KeyDown += (sender, e) =>
            {
                if (e.Key == Keys.Escape)
                {
                    this.Close(false);
                    e.Handled = true;
                }
            };    

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
                Padding = 10,
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
                box.CheckedChanged += (s, e) => bv.Value = box.Checked;
                inputBoolBoxes[bv.Name] = box;
                control = box;
            }

            Label label = new() { Text = settingsName };

            return new TableRow(new Label { Text = settingsName }, control);
        }

        TableRow CreateRow_LabelDrop_CustomText(
            string labelText,
            (string Key, string Text)[] options,
            string activateKey = "custom",
            string? initialKey = null)
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };

            var dropdown = new DropDown();
            foreach (var (key, text) in options)
                dropdown.Items.Add(new ListItem { Key = key, Text = text });

            // Select initial item
            if (!string.IsNullOrEmpty(initialKey) && options.Any(o => o.Key == initialKey))
                dropdown.SelectedKey = initialKey;
            else if (dropdown.Items.Count > 0)
                dropdown.SelectedIndex = 0;

            var customText = new TextBox
            {
                PlaceholderText = "Custom…",
                Width = 140
            };

            void Sync()
            {
                bool isActive = dropdown.SelectedKey == activateKey;
                customText.Enabled = isActive;
                customText.Visible = isActive;     // hide when inactive (optional)
                if (isActive) customText.Focus();
                else customText.Text = string.Empty; // optional clear
            }

            dropdown.SelectedIndexChanged += (_, __) => Sync();
            Sync(); // set initial state without using protected members

            return new TableRow(
                new TableCell(label),
                new TableCell(dropdown, scaleWidth: true),
                new TableCell(customText)
            );
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

        static ImageView CreateControllerImage()
        {
            Bitmap bmp;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Daxs.Shared.xBoxDummy.png"))
            {
                bmp = new Bitmap(stream);
            }

            return new ImageView
            {
                Image = bmp,
                Size = new Size(400, 250),
            };
        }
    }
}