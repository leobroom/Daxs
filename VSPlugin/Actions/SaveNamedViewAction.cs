using Daxs.Settings;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Display;
using Rhino.UI;
using System;

namespace Daxs.Actions
{
    internal class SaveNamedViewAction : ActionBase
    {
        private InputY _mode;

        public SaveNamedViewAction(InputX Input) : base(Input)
        { }

        public override string HUD_Text => $"Save current viewport as:";

        public override void Execute()
        {
            var doc = RhinoDoc.ActiveDoc;
            var activeView = doc.Views.ActiveView;     
            if (activeView == null)
            {
                RhinoApp.WriteLine("No active view.");
                return;
            }

            string currentName = activeView.ActiveViewport.Name;
            var dialog = new SaveNamedViewDialog(currentName);

            _hud.SetText(HUD_Emoji, HUD_Text);

            string result = dialog.ShowModal(RhinoEtoApp.MainWindowForDocument(doc));
            if (string.IsNullOrWhiteSpace(result))
            {
                RhinoApp.WriteLine("Cancelled.");
                return;
            }

            // Optional duplicate handling
            int existing = doc.NamedViews.FindByName(result);
            if (existing >= 0)
            {
                var overwrite = MessageBox.Show(
                    $"A named view called \"{result}\" already exists.\nDo you want to replace it?",
                    "Named View",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Question);

                if (overwrite != DialogResult.Yes)
                    return;

                doc.NamedViews.Delete(existing);
            }

            int index = doc.NamedViews.Add(result, activeView.ActiveViewport.Id);
            string msg = (index >= 0) ? $"Named view saved: {result}" : "Failed to save named view.";

            RhinoApp.WriteLine(msg);
        }

        private class SaveNamedViewDialog : Dialog<string>
        {
            private readonly TextBox _textBox;

            public SaveNamedViewDialog(string initialName)
            {
                Title = "Save Viewport As Named View";
                Resizable = false;
                Minimizable = false;
                Maximizable = false;
                ShowInTaskbar = false;

                //Padding = 10;
                Padding = new Padding(10, 10, 10, 0);
                ClientSize = new Size(287, 91);

                this.UseRhinoStyle();

                var label = new Label { Text = "Save current viewport settings as:" };

                _textBox = new TextBox
                {
                    Text = initialName ?? string.Empty
                };

                int bWidth = 80;

                var okButton = new Button
                {
                    Text = "OK",
                    Width = bWidth
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Width = bWidth
                };

                okButton.Click += (s, e) =>
                {
                    var name = (_textBox.Text ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show(
                            this,
                            "Please enter a name.",
                            "Named View",
                            MessageBoxButtons.OK,
                            MessageBoxType.Warning);
                        return;
                    }

                    Close(name);
                };

                cancelButton.Click += (s, e) => Close(null);

                DefaultButton = okButton;
                AbortButton = cancelButton;

                var buttonRow = new TableLayout
                {
                    Spacing = new Size(5, 0),
                    Rows =
            {
                new TableRow(null, okButton, cancelButton)
            }
                };

                Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
            {
                label,
                _textBox,
                buttonRow
            }
                };

                Shown += (s, e) =>
                {
                    _textBox.Focus();
                    _textBox.SelectAll();
                };
            }
        }
    }
}