using Eto.Drawing;
using Eto.Forms;
using Rhino.ApplicationSettings;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Daxs.Settings
{
    /// <summary>
    /// Static helper class to retrieve theme colors, making it accessible 
    /// from any component without needing an instance.
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Retrieves the System.Drawing.Color from Rhino's theme settings and converts it to Eto.Drawing.Color.
        /// </summary>
        public static Eto.Drawing.Color GetThemeColor(PaintColor colorKey)
        {
            try
            {
                // We must use the PaintColor enum from Rhino.ApplicationSettings to call GetPaintColor
                Rhino.ApplicationSettings.PaintColor rhinoPaintColor;

                // Safely convert our custom PaintColor enum value to the RhinoCommon one
                if (Enum.TryParse<Rhino.ApplicationSettings.PaintColor>(colorKey.ToString(), out rhinoPaintColor))
                {
                    System.Drawing.Color sysColor = AppearanceSettings.GetPaintColor(rhinoPaintColor);

                    // Convert System.Drawing.Color to Eto.Drawing.Color (needs float components)
                    return new Eto.Drawing.Color(
                        sysColor.R / 255f,
                        sysColor.G / 255f,
                        sysColor.B / 255f,
                        sysColor.A / 255f
                    );
                }
            }
            catch (Exception ex)
            {
                // Log or handle conversion error if needed, but return a safe color.
                Rhino.RhinoApp.WriteLine($"Error retrieving color {colorKey}: {ex.Message}");
            }
            // Fallback: Magenta indicates an error or unavailable color.
            return Colors.Magenta;
        }
    }

    /// <summary>
    /// A helper Eto Drawable control to visualize all colors defined in the PaintColor enumeration.
    /// </summary>
    public class ColorViewer : Drawable
    {
        private readonly Dictionary<string, Eto.Drawing.Color> colorMap = new Dictionary<string, Eto.Drawing.Color>();
        private const int CellSize = 80; // Width and height of each color cell

        public ColorViewer()
        {
            this.Size = new Size(600, 600);
            this.UseRhinoStyle();

            FetchAllColors();

            this.Paint += OnPaint;
        }

        private void FetchAllColors()
        {
            var values = Enum.GetValues(typeof(PaintColor)).Cast<PaintColor>();

            foreach (var colorKey in values)
            {
                Eto.Drawing.Color color = ThemeHelper.GetThemeColor(colorKey);
                colorMap.Add(colorKey.ToString(), color);
            }

            // Calculate minimum required size based on the number of colors
            int count = colorMap.Count;
            int columns = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / columns);
            this.Size = new Size(columns * CellSize + 20, rows * CellSize + 20); // +20 for padding
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.AntiAlias = true;

            int x = 10;
            int y = 10;
            int colIndex = 0;
            int columns = (int)Math.Ceiling(Math.Sqrt(colorMap.Count));

            Eto.Drawing.Font labelFont = new Eto.Drawing.Font(SystemFont.Default, 8);

            Eto.Drawing.Color panelBg = ThemeHelper.GetThemeColor(PaintColor.PanelBackground);
            Eto.Drawing.Color labelColor = (panelBg.Luminance() > 0.5) ? Colors.Black : Colors.White;

            foreach (var kvp in colorMap)
            {
                string name = kvp.Key;
                Eto.Drawing.Color color = kvp.Value;

                // 1. Draw the Color Cell Background
                g.FillRectangle(color, x, y, CellSize - 10, CellSize - 10);

                // 2. Draw the Color Cell Border
                g.DrawRectangle(labelColor.Mix(panelBg, 0.5f), x, y, CellSize - 10, CellSize - 10);

                // 3. Draw the Label (Name)
                Eto.Drawing.Color textFillColor = (color.Luminance() > 0.5) ? Colors.Black : Colors.White;

                // Draw name background for contrast
                g.FillRectangle(Colors.Black.WithAlpha(0.6f), x, y + CellSize - 35, CellSize - 10, 25);

                // Draw the color name text
                g.DrawText(labelFont, textFillColor, x + 5, y + CellSize - 33, name);

                // Move to the next cell
                colIndex++;
                if (colIndex >= columns)
                {
                    x = 10;
                    y += CellSize;
                    colIndex = 0;
                }
                else
                {
                    x += CellSize;
                }
            }
        }
    }
}
