
using Eto.Drawing;
using Eto.Forms;

namespace Daxs
{
    internal static class EtoFactory
    {
        internal static TableCell CreateLabel(string name) 
        {
            Label label = new()
            {
                Text = name,
                Width = 80
            };

            return new TableCell(label);
        }

        internal static DynamicLayout CreateLayout() 
        {
            return new DynamicLayout
            {
                Padding = new Padding(10, 0, 0, 0),
                Spacing = new Size(5, 5)
            };
        }

        internal static TableRow CreateControlRow(string labelName, Control control) 
        {
            var label = CreateLabel(labelName);
            return new TableRow(label, new TableCell(control, scaleWidth: true));
        }

        internal static TextBox CreateTextBox(TextValue tv) 
        {
            var box = new TextBox { Text = tv.Value, PlaceholderText = "Enter Textvalue" };
            box.TextChanged += (s, e) => tv.Value = box.Text;
            return box;
        }
    }
}