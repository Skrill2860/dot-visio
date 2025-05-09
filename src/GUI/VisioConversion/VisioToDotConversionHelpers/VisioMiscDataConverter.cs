using System.Linq;
using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion.VisioToDotConversionHelpers
{
    internal class VisioMiscDataConverter
    {
        public static string GetFontNameFromId(short fontId, Document doc)
        {
            foreach (Font font in doc.Fonts)
            {
                if (font.ID == fontId)
                    return font.Name;
            }

            return doc.Fonts[0].Name;
            // throw new ArgumentException($"Не найден шрифт в текущем документе: fontId = {fontId}");
        }

        public static string ColorFromFormula(string formula)
        {
            if (formula.StartsWith("RGB(") && formula.EndsWith(")"))
            {
                string trimmed = formula.Substring(4, formula.Length - 5); // example "255,255,255"
                int[] args = trimmed.Split(',', '.').Select(int.Parse).ToArray();
                if (args.Length == 3) // rgb
                {
                    return $"#{args[0]:X2}{args[1]:X2}{args[2]:X2}";
                }
            }
            return "#000000"; // fallback default
        }
    }
}
