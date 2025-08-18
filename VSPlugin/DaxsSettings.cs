// #! csharp
using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.UI;
using Rhino.UI.Controls;
using Daxs;
using System.Reflection;

namespace Daxs
{
    public class DaxsSettings : Dialog<bool>
    {
        private readonly Settings settings = Settings.Instance;
        private readonly Dictionary<string, NumericStepper> inputBoxes = new();
        private Button okButton;

        public DaxsSettings()
        {
            Title = "Daxs Gamepad Settings";
            ClientSize = new Size(400, 700);
            Resizable = false;

            TableLayout inputLayout = CreateNumericValues();
            ImageView cImage = CreateControllerImage();
            TableLayout dialogButtons = CreateDialogButtons();

            Content = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 10),
                Rows =
                {
                    new LabelSeparator { Text = "Gamepad Layout" },
                    cImage,
                    new LabelSeparator { Text = "Input Response" },
                    inputLayout,
                    new LabelSeparator { Text = "Button Mapping" },
                    dialogButtons
                }
            };

            // Handle key down for Escape
            this.KeyDown += (sender, e) =>
            {
                if (e.Key == Eto.Forms.Keys.Escape)
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
            foreach (var nv in settings.AllValues)
            {
                nv.Reset();
                if (inputBoxes.TryGetValue(nv.Name, out var box))
                    box.Value = nv.DisplayValue;
            }
        }

        TableLayout CreateNumericValues()
        {
            var inputRows = new List<TableRow>();
            foreach (var nv in settings.AllValues)
            {
                var box = new NumericStepper { Value = nv.DisplayValue };
                box.ValueChanged += (s, e) => nv.DisplayValue = box.Value;
                inputBoxes[nv.Name] = box;

                inputRows.Add(new TableRow(new Label { Text = nv.Name }, box));
            }

            var inputLayout = new TableLayout
            {
                Padding = 10,
                Spacing = new Size(5, 5)
            };

            foreach (var row in inputRows)
                inputLayout.Rows.Add(row);

            return inputLayout;
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