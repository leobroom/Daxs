using System;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.UI;

// Rhino 8 ScriptEditor C# script
var doc = __rhino_doc__;
if (doc == null)
{
    RhinoApp.WriteLine("No active Rhino document.");
    return;
}

var activeView = doc.Views.ActiveView;
if (activeView == null)
{
    RhinoApp.WriteLine("No active view.");
    return;
}

string initialName = activeView.ActiveViewport.Name;
var dlg = new SaveNamedViewDialog(initialName);

string result = dlg.ShowModal(RhinoEtoApp.MainWindowForDocument(doc));

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

if (index >= 0)
    RhinoApp.WriteLine($"Named view saved: {result}");
else
    RhinoApp.WriteLine("Failed to save named view.");

public class SaveNamedViewDialog : Dialog<string>
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
        Padding = new Padding(10,10,10,0);
        ClientSize = new Size(290, 91);

        this.UseRhinoStyle();

        var label = new Label
        {
            Text = "Save current viewport settings as:"
        };

        _textBox = new TextBox
        {
            Text = initialName ?? string.Empty
        };


int w = 100;
        var okButton = new Button
        {
            Text = "OK",
            Width = w
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Width = w
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
            Spacing = new Size(0, 0),
            Rows =
            {
                new TableRow(null, okButton, cancelButton, null)
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