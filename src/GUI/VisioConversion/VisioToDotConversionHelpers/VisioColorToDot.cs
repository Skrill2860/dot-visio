using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion.VisioToDotConversionHelpers;

public static class VisioColorToDot
{
    public static string RgbFromPalette(Colors palette, int color)
    {
        try
        {
            var r = palette[color].Red.ToString("X2");
            var g = palette[color].Green.ToString("X2");
            var b = palette[color].Blue.ToString("X2");
            return "#" + r + g + b;
        }
        catch
        {
            return "#FF0000";
        }
    }
}