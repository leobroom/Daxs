
using Eto.Drawing;
using Eto.Forms;
using System;

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

        internal static DynamicLayout CreateLayout(int leftPadding = 10, int topPadding = 0, int rightPadding = 0, int bottomPadding = 0)
        {
            return new DynamicLayout
            {
                Padding = new Padding(leftPadding, topPadding, rightPadding, bottomPadding),
                Spacing = new Size(5, 5)
            };
        }


        internal static TableRow CreateControlRow(string labelName, Control control) 
        {
            var label = CreateLabel(labelName);
            return new TableRow(label, new TableCell(control, scaleWidth: true));
        }

        internal static Expander CreateExpander(string title, Control content, bool expanded = false)
        {
            var label = new Label { Text = title };
            //if (bold)
            //    label.Font = SystemFonts.Bold(SystemFonts.Default().Size + 1);

            return new Expander
            {
                Header = label,
                Content = content,
                Expanded = expanded,
                Padding = new Padding(0, 0, 0, 0),
            };
        }

        internal static DynamicLayout CreateGroup(string[] names, Func<string, TableRow> controlFactory)
        {
            var layout = CreateLayout();
            foreach (var name in names)
                layout.Add(controlFactory(name));
            return layout;
        }

        internal static TableRow CreateGroupExpander(string title, string[] names, Func<string, TableRow> controlFactory, bool expanded)
        {
            var layout = CreateGroup(names, controlFactory);
            var expander = CreateExpander(title, layout,expanded);
            return new TableRow(expander);
        }

        internal static TableRow CreateButtonRow((string text, Action onClick)[] buttons)
        {
            var layout = new TableLayout { Padding = 10, Spacing = new Size(5, 5) };
            var row = new TableRow();
            foreach (var (text, onClick) in buttons)
                row.Cells.Add(new TableCell(new Button { Text = text, Command = new Command((_, _) => onClick()) }, false));
            layout.Rows.Add(null);
            layout.Rows.Add(row);
            return new TableRow(layout);
        }

        internal static Expander CreateCustomExpander(string header, DynamicLayout content, TextBox boundTextBox)
        {
            var label = new Label { Text = header };
            string MakeHeader() => $"{header}{(string.IsNullOrEmpty(boundTextBox.Text) ? "" : " - ")}{boundTextBox.Text}";
            boundTextBox.TextChanged += (_, _) => label.Text = MakeHeader();

            return new Expander
            {
                Header = label,
                Content = content,
                Expanded = false,
                Padding = new Padding(0, 0, 0, 0)
            };
        }

        internal static TableRow CreateContentExpander(string title, Control content, bool expanded = false)
        {
            var expander = CreateExpander(title, content, expanded);
            return new TableRow(expander);
        }

    }
}