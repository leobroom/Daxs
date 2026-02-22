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

        internal static TableRow CreateButtonButtons((string text, Action onClick)[] buttons)
        {
            var layout = new TableLayout { Padding = 10, Spacing = new Size(5, 5) };
            var row = new TableRow();
            foreach (var (text, onClick) in buttons)
            {
                TableCell tc = (onClick != null) ? new TableCell(new Button { Text = text, Command = new Command((_, _) => onClick()) }, false) : new TableCell { ScaleWidth = true };

                row.Cells.Add(tc);
            }
               
            layout.Rows.Add(null);
            layout.Rows.Add(row);
            return new TableRow(layout);
        }

        internal static TableRow CreateContentExpander(string title, Control content, bool expanded = false)
        {
            var expander = CreateExpander(title, content, expanded);
            return new TableRow(expander);
        }

        //TABS

        internal static TabPage CreateAboutTab()
        {
            var githubLink = new Label
            {
                Text =
                $"Daxs {Utils.GetPackageVersion()}\n" +
                "View on GitHub",
                Cursor = Cursors.Pointer,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextAlignment = TextAlignment.Center
            };

            githubLink.MouseDown += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/leobroom/Daxs", UseShellExecute = true });
                }
                catch { }
            };

            string licensePth = Utils.GetSharedFile("LICENSE.txt");
            string licenseText = "License file not found."; if (File.Exists(licensePth))
            {
                using var reader = new StreamReader(licensePth); 
                string rawText = reader.ReadToEnd(); 
                                                                                                    
                rawText = rawText.Replace("\r\n", "\n"); 
                rawText = rawText.Replace("\n\n", "[[PARA]]"); 
                rawText = rawText.Replace("\n", " "); 
                licenseText = rawText.Replace("[[PARA]]", "\n\n");
            }

            var licenseLabel = new Label
            {
                Text = licenseText,
                Wrap = WrapMode.Word,
                TextAlignment = TextAlignment.Left,
                //TextColor = Colors.Gray,
                //BackgroundColor = Colors.Red,
            };

            Control[] rows = new Control[] 
            { 
                new TableRow(githubLink), 
                new TableRow { ScaleHeight = true },
                new TableRow(licenseLabel) { ScaleHeight = true }
            };

            return CreateTabpage("ℹ️ About", rows);
        }

        internal static TabPage CreateThemeTab() 
        {
            var colorViewer = new ColorViewer();
            return CreateTabpage("🎨 Theme Colors", new Control[] { new TableRow(colorViewer) });
        }

        internal static TabPage CreateTabpage(string tabTitle, Control[] rows) 
        {
            var layout = new DynamicLayout { Padding = new Padding(10,10,10,10), Spacing = new Size(10, 10), Width = 420 };
            
            if (rows != null)
                layout.AddRange(rows);

            Scrollable scroll = new ()
            {
                Content = layout,
                ExpandContentWidth = true,
                ExpandContentHeight = false,
                Border = BorderType.None,
            };

            TabPage inputTab = new ()
            {
                Text = tabTitle,
                Content = scroll
            };

            return inputTab;
        }
    }
}