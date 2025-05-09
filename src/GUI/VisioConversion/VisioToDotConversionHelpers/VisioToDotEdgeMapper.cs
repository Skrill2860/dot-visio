using System.Collections.Generic;
using System.Globalization;
using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion.VisioToDotConversionHelpers;

public static class VisioToDotEdgeMapper
{
    public static Dictionary<string, string> ExtractDotAttributes(Shape connector)
    {
        var attrs = new Dictionary<string, string>();

        // Label (text)
        if (!string.IsNullOrWhiteSpace(connector.Text))
        {
            attrs["label"] = connector.Text;
        }

        // Font name
        if (connector.CellExistsU["Char.Font", 0] != 0)
        {
            short fontId = (short)connector.CellsU["Char.Font"].ResultInt["", 0];
            string fontName = VisioMiscDataConverter.GetFontNameFromId(fontId, connector.Document);
            attrs["fontname"] = fontName;
        }

        // Font size
        if (connector.CellExistsU["Char.Size", 0] != 0)
        {
            attrs["fontsize"] = connector.CellsU["Char.Size"].FormulaU;
        }

        // Font color
        if (connector.CellExistsU["Char.Color", 0] != 0)
        {
            attrs["fontcolor"] = VisioColorToDot.RgbFromPalette(connector.Document.Colors, connector.Cells["Char.Color"].ResultInt[VisUnitCodes.visUnitsColor, -1]);
        }

        // Border color
        if (connector.CellExistsU["LineColor", 0] != 0)
        {
            attrs["color"] = VisioColorToDot.RgbFromPalette(connector.Document.Colors, connector.Cells["LineColor"].ResultInt[VisUnitCodes.visUnitsColor, -1]);
        }

        // Line weight
        if (connector.CellExistsU["LineWeight", 0] != 0)
        {
            attrs["penwidth"] = connector.CellsU["LineWeight"].ResultIU.ToString(CultureInfo.InvariantCulture);
        }

        // Line style (dashed, dotted)
        if (connector.CellExistsU["LinePattern", 0] != 0)
        {
            var pattern = (int)connector.CellsU["LinePattern"].ResultIU;
            if (pattern == 2)
            {
                attrs["style"] = "dashed";
            }
            else if (pattern == 3)
            {
                attrs["style"] = "dotted";
            }
        }

        // Arrowheads
        if (connector.CellExistsU["BeginArrow", 0] != 0)
        {
            var arrowCode = (int)connector.CellsU["BeginArrow"].ResultIU;
            if (arrowCode != 0)
            {
                attrs["arrowtail"] = ArrowIndexToDot(arrowCode);
            }
        }

        if (connector.CellExistsU["EndArrow", 0] != 0)
        {
            var arrowCode = (int)connector.CellsU["EndArrow"].ResultIU;
            if (arrowCode != 0)
            {
                attrs["arrowhead"] = ArrowIndexToDot(arrowCode);
            }
        }

        // Arrow size
        if (connector.CellExistsU["EndArrowSize", 0] != 0)
        {
            attrs["arrowsize"] = connector.CellsU["EndArrowSize"].ResultIU.ToString(CultureInfo.InvariantCulture);
        }

        // Tooltip as comment
        if (connector.CellExistsU["Comment", 0] != 0)
        {
            attrs["tooltip"] = connector.CellsU["Comment"].FormulaU.Trim('"');
        }

        // Hyperlink
        if (connector.Hyperlinks.Count > 0)
        {
            var link = connector.Hyperlinks[1];
            if (!string.IsNullOrEmpty(link.Address))
            {
                attrs["URL"] = link.Address;
            }
        }

        return attrs;
    }

    private static string ArrowIndexToDot(int index)
    {
        return index switch
        {
            0 => "none",
            1 => "dot",
            2 => "normal",
            3 => "inv",
            5 => "vee",
            _ => "normal"
        };
    }
}