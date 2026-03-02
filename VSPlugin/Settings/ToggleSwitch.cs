using System;
using Eto.Drawing;
using Eto.Forms;
using Rhino.ApplicationSettings;
using Rhino.UI;

namespace Daxs.Settings
{
    public class ToggleSwitch : Drawable
    {
        public event EventHandler<bool> Toggled;

        private bool isOn;
        private bool isHover;

        public bool IsOn
        {
            get => isOn;
            set { 
                if (isOn == value) 
                    return; 
                isOn = value; 
                Invalidate(); 
                Toggled?.Invoke(this, isOn); 
            }
        }

        public ToggleSwitch()
        {
            this.UseRhinoStyle();

            Size = new Size(100, 26);
            Cursor = Cursors.Pointer;
            BackgroundColor = Colors.Transparent;

            MouseDown += (_, __) => IsOn = !IsOn;
            MouseEnter += (_, __) => 
            { 
                isHover = true; 
                Invalidate(); 
            };

         

            MouseLeave += (_, __) => 
            { 
                isHover = false; 
                Invalidate(); 
            };

            Paint += (sender, e) =>
            {
                var g = e.Graphics;
                g.AntiAlias = true;


                var surface = GetThemeColor(PaintColor.PanelBackground);    
                var textColor = GetThemeColor(PaintColor.TextEnabled);       
                var inactiveBg = GetThemeColor(PaintColor.PanelBackground); 
                var borderColor = GetThemeColor(PaintColor.NormalBorder);   
                var hoverTint = GetThemeColor(PaintColor.PressedBorder);        
                var activeBg = GetThemeColor(PaintColor.NormalStart);

                var knobFill = IsOn ? Colors.LightGreen : Colors.Red;
                var bgColor = IsOn ? activeBg : inactiveBg;

                float r = Height / 2f;
                var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

                using (var path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, 2 * r, 2 * r, 180, 90);
                    path.AddArc(rect.Right - 2 * r, rect.Y, 2 * r, 2 * r, 270, 90);
                    path.AddArc(rect.Right - 2 * r, rect.Bottom - 2 * r, 2 * r, 2 * r, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - 2 * r, 2 * r, 2 * r, 90, 90);
                    path.CloseFigure();

             
                    if (isHover)
                    {
                        g.FillPath(new SolidBrush(borderColor), path);
                        g.DrawPath(new Pen(hoverTint, 1f), path);
                    }
                    else 
                    {
                        g.FillPath(new SolidBrush(bgColor), path);
                        g.DrawPath(new Pen(borderColor, 1f), path);
                    }
                }
 
                // Knob
                float m = 3f;
                float d = Height - 2 * m;
                float knobX = IsOn ? Width - d - m - 1 : m;
                var knobRect = new RectangleF(knobX, m, d, d);


                if (isHover)
                {
                    g.FillEllipse(new SolidBrush(knobFill), knobRect);
                    g.DrawEllipse(new Pen(hoverTint, 1f), knobRect);
                }
                else
                {
                    g.FillEllipse(new SolidBrush(knobFill), knobRect);
                    g.DrawEllipse(new Pen(borderColor, 1f), knobRect);
                }

                // Text
                string label = IsOn ? "ON" : "OFF";
                var labelColor = Colors.White.Mix(textColor.ContrastColor(), 0.2f); // ensure readable
                var font = new Font(SystemFont.Default, 9);
                float tx = IsOn ? m + 9 : Width - 34;
                g.DrawText(font, labelColor, tx, Height / 2f - 8, label);

            };
        }

        public Color GetThemeColor(PaintColor pc)
        {
            // This color is theme-aware and should return the correct light or dark panel background.
            return AppearanceSettings.GetPaintColor(pc).ToEto();
        }
    }

    public static class ColorExtensions
    {
        public static Color Mix(this Color a, Color b, float t)
            => new Color(
                a.R + (b.R - a.R) * t,
                a.G + (b.G - a.G) * t,
                a.B + (b.B - a.B) * t,
                a.A + (b.A - a.A) * t);

        public static Color WithAlpha(this Color c, float a) => new Color(c.R, c.G, c.B, a);

        public static double Luminance(this Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;

        /// Light themes → black-ish; dark themes → white-ish
        public static Color ContrastColor(this Color baseColor)
            => (baseColor.Luminance() >= 0.5) ? new Color(0, 0, 0) : new Color(1, 1, 1);
    }

    // You will need a helper extension method to convert System.Drawing.Color 
    // (which AppearanceSettings returns) to Eto.Drawing.Color.
    public static class ColorConversionExtensions
    {
        public static Eto.Drawing.Color ToEto(this System.Drawing.Color sysColor)
        {
            return new Eto.Drawing.Color(
                sysColor.R / 255f,
                sysColor.G / 255f,
                sysColor.B / 255f,
                sysColor.A / 255f
            );
        }
    }


}
