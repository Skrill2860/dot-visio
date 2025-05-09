using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GUI.Common;
using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion.VisioToDotConversionHelpers;

public static class VisioToDotNodeMapper
{
    public static Dictionary<string, string> ExtractDotAttributes(Shape shape)
    {
        var attrs = new Dictionary<string, string>();

        if (SharedGui.CurrentDotSettings["exportpositions"] != "false")
        {
            var exportArgs = SharedGui.CurrentDotSettings["exportpositions"].Split('_').ToArray();
            var usePointsModifier = exportArgs.LastOrDefault() == "points" ? 72 : 1;
            var forcedPositionSymbol = exportArgs.FirstOrDefault() == "forced" ? "!" : "";

            var graphvizX = shape.CellsU["PinX"].ResultIU * usePointsModifier;
            var graphvizY = shape.CellsU["PinY"].ResultIU * usePointsModifier;


            attrs["pos"] = string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F4}{2:forcedPositionSymbol}",
                graphvizX, graphvizY, forcedPositionSymbol);
        }
        
        // Label (text)
        if (!string.IsNullOrWhiteSpace(shape.Text))
        {
            attrs["label"] = shape.Text;
        }

        // Invis
        var linePattern = (int)shape.CellsU["LinePattern"].ResultIU;
        var fillPattern = (int)shape.CellsU["FillPattern"].ResultIU;
        if (linePattern == 0 && fillPattern == 0)
        {
            attrs["style"] = "invis";
        }

        // Font name
        if (shape.CellExistsU["Char.Font", 0] != 0)
        {
            var fontId = (short)shape.CellsU["Char.Font"].ResultInt["", 0];
            var fontName = VisioMiscDataConverter.GetFontNameFromId(fontId, shape.Document);
            attrs["fontname"] = fontName;
        }

        // Font size
        if (shape.CellExistsU["Char.Size", 0] != 0)
        {
            attrs["fontsize"] = shape.CellsU["Char.Size"].FormulaU;
        }

        // Font color
        if (shape.CellExistsU["Char.Color", 0] != 0)
        {
            attrs["fontcolor"] = VisioColorToDot.RgbFromPalette(shape.Document.Colors, shape.Cells["Char.Color"].ResultInt[VisUnitCodes.visUnitsColor, -1]);
        }

        // Fill color
        if (shape.CellExistsU["FillForegnd", 0] != 0)
        {
            attrs["fillcolor"] = VisioColorToDot.RgbFromPalette(shape.Document.Colors, shape.Cells["FillForegnd"].ResultInt[VisUnitCodes.visUnitsColor, -1]);
        }

        // Border color
        if (shape.CellExistsU["LineColor", 0] != 0)
        {
            attrs["color"] = VisioColorToDot.RgbFromPalette(shape.Document.Colors, shape.Cells["LineColor"].ResultInt[VisUnitCodes.visUnitsColor, -1]);
        }

        // Border thickness
        if (shape.CellExistsU["LineWeight", 0] != 0)
        {
            attrs["penwidth"] = shape.CellsU["LineWeight"].ResultIU.ToString(CultureInfo.InvariantCulture);
        }

        // Width & height (in inches)
        if (shape.CellExistsU["Width", 0] != 0)
        {
            attrs["width"] = shape.CellsU["Width"].ResultIU.ToString(CultureInfo.InvariantCulture);
        }

        if (shape.CellExistsU["Height", 0] != 0)
        {
            attrs["height"] = shape.CellsU["Height"].ResultIU.ToString(CultureInfo.InvariantCulture);
        }

        // Tooltip
        if (shape.CellExistsU["Comment", 0] != 0)
        {
            attrs["tooltip"] = shape.CellsU["Comment"].FormulaU.Trim('"');
        }

        // Shape style (type)
        if (!string.IsNullOrEmpty(shape.Master?.NameU))
        {
            var masterName = shape.Master!.Name.ToLowerInvariant();
            attrs["shape"] = MasterToDotShape(masterName);
        }

        // Hyperlink
        if (shape.Hyperlinks.Count > 0)
        {
            var link = shape.Hyperlinks.ItemU[0];
            if (!string.IsNullOrEmpty(link.Address))
            {
                attrs["URL"] = link.Address;
            }
        }

        var transparency = shape.Cells["FillForegndTrans"].ResultIU;
        if (transparency < 100d)
        {
            if (attrs.TryGetValue("style", out string existingStyle))
            {
                if (existingStyle != "invis")
                {
                    attrs["style"] = $"{existingStyle},filled";
                }
            }
            else
            {
                attrs.Add("style", "filled");
            }
        }

        return attrs;
    }

    private static string MasterToDotShape(string masterName)
    {
        return masterName switch
        {
            "rectangle" => "box",
            "ellipse" => "ellipse",
            "circle" => "circle",
            "doublecircle" => "doublecircle",
            "diamond" => "diamond",
            "parallelogram" => "parallelogram",
            _ => "box"
        };
    }
}