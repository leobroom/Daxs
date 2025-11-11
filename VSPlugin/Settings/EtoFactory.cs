
using Eto.Drawing;
using Eto.Forms;
using System;
using System.IO;

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



        internal static TableRow CreateContentExpander(string title, Control content, bool expanded = false)
        {
            var expander = CreateExpander(title, content, expanded);
            return new TableRow(expander);
        }

        internal static TabPage CreateAboutTab() 
        {
            // --- ABOUT TAB ---
            var aboutLayout = new DynamicLayout { Padding = 10, Spacing = new Size(10, 10) };

            // Github

            var githubLink = new Label
            {
                Text = "View on GitHub",
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

            aboutLayout.Add(new TableRow(githubLink));
            aboutLayout.Add(new TableRow { ScaleHeight = true });

            string licensePth = Utils.GetSharedFile("LICENSE.txt");

            string licenseText = "License file not found.";

            if (File.Exists(licensePth))
            {
                using var reader = new StreamReader(licensePth);
                string rawText = reader.ReadToEnd();

                // Normalize newlines (handle Windows and Unix endings)
                rawText = rawText.Replace("\r\n", "\n");

                // Replace double newlines with a placeholder
                rawText = rawText.Replace("\n\n", "[[PARA]]");

                // Replace remaining single newlines with spaces
                rawText = rawText.Replace("\n", " ");

                // Restore double newlines as real breaks
                licenseText = rawText.Replace("[[PARA]]", "\n\n");
            }


            aboutLayout.Add(new Label
            {
                Text = licenseText,
                Wrap = WrapMode.Word,
                TextAlignment = TextAlignment.Left,
                TextColor = Colors.Gray
            });

            return  new TabPage
            {
                Text = "ℹ️ About",
                Content = aboutLayout
            };
        }

        internal static TabPage CreateThemeTab() 
        {
            var colorViewer = new ColorViewer();
            var themeScroll = new Scrollable
            {
                Content = colorViewer,
                ExpandContentWidth = true,
                ExpandContentHeight = false,
                Border = BorderType.None
            };
            return new TabPage
            {
                Text = "🎨 Theme Colors",
                Content = themeScroll
            };
        }
    }
}