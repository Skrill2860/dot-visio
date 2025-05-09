using System;
using System.Drawing;
using System.Globalization;
using GUI.Error_Handling;
using ColorConverter = Common.ColorConverter;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class ColorToVisioRgbConverter
{
    public static string ColorToVisioRgbFormula(string colorString)
    {
        var r = 127;
        var g = 127;
        var b = 127;
        var color = colorString;
        try
        {
            color = color.Replace(",", " "); // Commas are allowed in HSV: "H[, ]+S[, ]+V"
            if (color.StartsWith("#"))
            {
                r = Convert.ToInt32(color.Substring(1, 2), 16);
                g = Convert.ToInt32(color.Substring(3, 2), 16);
                b = Convert.ToInt32(color.Substring(5, 2), 16);
            }
            else if (color.Contains(" ")) // HSV
            {
                var args = color.Split(' ');
                var hsv = new ColorConverter.Hsv
                {
                    H = Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    S = Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                    V = Convert.ToDouble(args[2], CultureInfo.InvariantCulture)
                };
                var answer = ColorConverter.ColorFromHsv(hsv);
                r = answer.R;
                g = answer.G;
                b = answer.B;
            }
            else // named using the X11 standard
            {
                color = color.Replace("grey", "gray");
                var modifier = color.Substring(color.Length - 1, 1);
                if ((string.Compare(modifier, "1", StringComparison.OrdinalIgnoreCase) >= 0) &
                    (string.Compare(modifier, "4", StringComparison.OrdinalIgnoreCase) <= 0))
                {
                    color = color.Substring(0, color.Length - 1);
                }
                else
                {
                    modifier = "";
                }

                Color rgbColor = Color.FromName(color);
                
                r = rgbColor.R;
                g = rgbColor.G;
                b = rgbColor.B;

                var multiplier = modifier switch
                {
                    "2" => 0.932d,
                    "3" => 0.804d,
                    "4" => 0.548d,
                    _ => 1d
                };

                if (multiplier < 1d)
                {
                    r = (int)Math.Round(r * multiplier);
                    g = (int)Math.Round(g * multiplier);
                    b = (int)Math.Round(b * multiplier);
                }
            }
        }
        catch
        {
            WarningDialogHelper.ShowWarning("Color `" + colorString + "` is invalid, color replaced with gray");
        }

        return "RGB(" + r + "," + g + "," + b + ")";
    }
}