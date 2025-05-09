using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Domain;
using GUI.Common;
using Microsoft.Office.Interop.Visio;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class ShapeRenderHelper
{
    public static void ApplyClusterAttributes(Graph graph)
    {
        var shape = graph.Shape;
        var attrs = graph.Attributes;

        if (shape == null || attrs.Count == 0)
        {
            return;
        }

        // Label
        if (attrs.TryGetValue("label", out var label))
        {
            shape.Text = SubstituteDotMarkersHelper.SubstituteDot(graph, label);
            if (graph.Attributes.TryGetValue("lp", out var lps))
            {
                if (!string.IsNullOrEmpty(lps))
                {
                    var bb = BoundingBoxParser.ParseBoundingBox(graph.Attributes["bb"]);
                    var lp = LabelPositionParser.ParseLabelPosition(lps);

                    var pinX = Convert.ToString(lp[0] - bb.MinX, CultureInfo.InvariantCulture) + " IN";
                    var pinY = Convert.ToString(lp[1] - bb.MinY, CultureInfo.InvariantCulture) + " IN";
                    shape.CellsU["TxtPinX"].FormulaForceU = pinX;
                    shape.CellsU["TxtPinY"].FormulaForceU = pinY;
                }
            }
        }

        // Font settings
        if (attrs.TryGetValue("fontname", out var fontname))
        {
            shape.CellsU["Char.Font"].FormulaU = $"\"{fontname}\"";
        }

        if (attrs.TryGetValue("fontsize", out var fontsizeStr) &&
            double.TryParse(fontsizeStr, out var fontsize))
        {
            shape.CellsU["Char.Size"].ResultIU = fontsize;
        }

        if (attrs.TryGetValue("fontcolor", out var fontcolor))
        {
            shape.CellsU["Char.Color"].FormulaU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(fontcolor);
        }

        // Border and Fill
        if (attrs.TryGetValue("bgcolor", out var bgColor))
        {
            shape.CellsU["FillForegnd"].FormulaU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(bgColor);
            shape.CellsU["FillPattern"].ResultIU = 1; // fallback
        }

        if (!attrs.TryGetValue("style", out var style))
        {
            style = "";
        }

        ApplyStyleString(shape, style);

        var styles = style.Split(',').Select(s => s.ToLower()).ToArray();
        if (!attrs.TryGetValue("fillcolor", out var fillColor))
        {
            fillColor = "";
        }

        if (!attrs.TryGetValue("color", out var borderColor))
        {
            borderColor = "";
        }

        if (!string.IsNullOrEmpty(borderColor))
        {
            if (borderColor == "transparent")
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                    (short)VisCellIndices.visLineColorTrans].FormulaForceU = "100%";
            }
            else
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                    (short)VisCellIndices.visLineColor].FormulaForceU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(borderColor);
            }
        }

        if (styles.Contains("filled") || !string.IsNullOrEmpty(fillColor))
        {
            if (fillColor == "transparent")
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowFill,
                    (short)VisCellIndices.visFillForegndTrans].FormulaForceU = "100%";
            }
            else
            {
                fillColor = !string.IsNullOrEmpty(fillColor) ? fillColor : !string.IsNullOrEmpty(borderColor) ? borderColor : "lightgrey";

                shape.CellsU["FillForegnd"].FormulaForceU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(fillColor);
            }
        }

        // Tooltip
        if (attrs.TryGetValue("tooltip", out var tooltip))
        {
            shape.CellsU["Comment"].FormulaU = $"\"{tooltip}\"";
        }

        // URL
        if (attrs.TryGetValue("URL", out var url))
        {
            var hyperlink = shape.Hyperlinks.Add();
            hyperlink.Address = url;
        }

        // Label Justification
        if (attrs.TryGetValue("labeljust", out var labeljust))
        {
            shape.CellsU["Para.HorzAlign"].ResultIU = labeljust.ToLower() switch
            {
                "l" or "left" => 0,
                "r" or "right" => 2,
                _ => 1 // Center default
            };
        }

        // Label Vertical Placement
        if (attrs.TryGetValue("labelloc", out var labelloc))
        {
            shape.CellsU["VerticalAlign"].ResultIU = labelloc.ToLower() switch
            {
                "t" or "top" => 0,
                "b" or "bottom" => 2,
                _ => 1 // Center default
            };
        }
    }

    public static void ApplyAttributesToShape(Node node)
    {
        var shape = node.Shape;
        var attrs = node.Attributes;

        if (shape == null || attrs.Count == 0)
        {
            return;
        }

        // Label
        if (attrs.TryGetValue("label", out var label))
        {
            shape.Text = SubstituteDotMarkersHelper.SubstituteDot(node.Graph, node, label);
        }

        // Font name
        if (attrs.TryGetValue("fontname", out var fontNames))
        {
            foreach (var fontName in fontNames.Split(','))
            {
                try
                {
                    var fntObjs = SharedGui.MyVisioApp.ActiveDocument.Fonts;
                    int fontIndex = fntObjs[fontName].Index;
                    shape.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                        (short)VisCellIndices.visCharacterFont].FormulaForceU = fontIndex.ToString();

                    break;
                }
                catch
                {
                    // ignored
                }
            }
        }

        // Font size
        if (attrs.TryGetValue("fontsize", out var fontsizeStr) &&
            double.TryParse(fontsizeStr, out var fontsize))
        {
            var fontsizeFormula = Convert.ToString(fontsize, CultureInfo.InvariantCulture) + " pt";

            shape.EnsureSection((short)VisSectionIndices.visSectionCharacter);
            shape.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                (short)VisCellIndices.visCharacterSize].FormulaForceU = fontsizeFormula;
        }

        // Font color
        if (attrs.TryGetValue("fontcolor", out var fontcolor))
        {
            shape.CellsU["Char.Color"].FormulaU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(fontcolor);
        }


        // Fill color && Border color
        if (!attrs.TryGetValue("style", out var style))
        {
            style = "";
        }

        ApplyStyleString(shape, style);

        var styles = style.Split(',').Select(s => s.ToLower()).ToArray();
        if (!attrs.TryGetValue("fillcolor", out var fillColor))
        {
            fillColor = "";
        }

        if (!attrs.TryGetValue("color", out var borderColor))
        {
            borderColor = "";
        }

        if (styles.Contains("filled") || !string.IsNullOrEmpty(fillColor) || !string.IsNullOrEmpty(borderColor))
        {
            if (fillColor == "transparent")
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowFill,
                    (short)VisCellIndices.visFillForegndTrans].FormulaForceU = "100%";
            }
            else
            {
                if (string.IsNullOrEmpty(fillColor))
                {
                    fillColor = borderColor;
                }

                if (string.IsNullOrEmpty(fillColor))
                {
                    fillColor = "lightgrey";
                }

                shape.CellsU["FillForegnd"].FormulaForceU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(fillColor);
            }
        }

        if (!string.IsNullOrEmpty(borderColor))
        {
            if (borderColor == "transparent")
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                    (short)VisCellIndices.visLineColorTrans].FormulaForceU = "100%";
            }
            else
            {
                shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                    (short)VisCellIndices.visLineColor].FormulaForceU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(borderColor);
            }
        }

        // Border width
        if (attrs.TryGetValue("penwidth", out var penwidthStr) &&
            double.TryParse(penwidthStr, out var penwidth))
        {
            shape.CellsU["LineWeight"].ResultIU = penwidth;
        }

        // Width & height
        if (attrs.TryGetValue("width", out var widthStr) &&
            double.TryParse(widthStr, out var width))
        {
            shape.CellsU["Width"].ResultIU = width;
        }

        if (attrs.TryGetValue("height", out var heightStr) &&
            double.TryParse(heightStr, out var height))
        {
            shape.CellsU["Height"].ResultIU = height;
        }

        // Tooltip
        if (attrs.TryGetValue("tooltip", out var tooltip))
        {
            shape.CellsU["Comment"].FormulaU = $"\"{tooltip}\"";
        }

        // Hyperlink
        if (attrs.TryGetValue("URL", out var url))
        {
            var hyperlink = shape.Hyperlinks.Add();
            hyperlink.Address = url;
        }
    }

    public static void ApplyAttributesToShape(Edge edge)
    {
        var attrs = edge.Attributes;
        var connector = edge.Shape;

        if (!attrs.Any() || connector == null)
        {
            return;
        }

        // Label
        if (attrs.TryGetValue("label", out var label))
        {
            connector.Text = SubstituteDotMarkersHelper.SubstituteDot(edge.Graph, edge, label);
        }

        // Font name
        if (attrs.TryGetValue("fontname", out var fontName))
        {
            try
            {
                var fntObjs = SharedGui.MyVisioApp.ActiveDocument.Fonts;
                int fontIndex = fntObjs[fontName].Index;
                connector.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                    (short)VisCellIndices.visCharacterFont].FormulaForceU = fontIndex.ToString();
            }
            catch
            {
                // ignored
            }
        }

        // Font size
        if (attrs.TryGetValue("fontsize", out var fontsizeStr) &&
            double.TryParse(fontsizeStr, out var fontsize))
        {
            var fontsizeFormula = Convert.ToString(fontsize, CultureInfo.InvariantCulture) + " pt";

            connector.CellsSRC[(short)VisSectionIndices.visSectionCharacter, (short)VisRowIndices.visRowCharacter,
                (short)VisCellIndices.visCharacterSize].FormulaForceU = fontsizeFormula;
        }

        // Font color
        if (attrs.TryGetValue("fontcolor", out var fontcolor))
        {
            connector.CellsU["Char.Color"].FormulaU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(fontcolor);
        }

        // Line color
        if (attrs.TryGetValue("color", out var lineColor))
        {
            connector.CellsU["LineColor"].FormulaU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(lineColor);
        }
        else
        {
            connector.CellsU["LineColor"].FormulaU = "RGB(0,0,0)";
        }

        // Pen width
        if (attrs.TryGetValue("penwidth", out var penwidthStr) &&
            double.TryParse(penwidthStr, out var penwidth))
        {
            connector.CellsU["LineWeight"].ResultIU = penwidth;
        }

        // Style
        if (attrs.TryGetValue("style", out var style))
        {
            if (style.Contains("dashed"))
            {
                connector.CellsU["LinePattern"].ResultIU = 2;
            }
            else if (style.Contains("dotted"))
            {
                connector.CellsU["LinePattern"].ResultIU = 3;
            }
        }

        // Arrowhead and tail
        if (attrs.TryGetValue("arrowhead", out var arrowhead))
        {
            connector.CellsU["EndArrow"].ResultIU = ArrowNameToIndex(arrowhead);
        }
        else if (edge.Graph.IsDigraph)
        {
            connector.CellsU["EndArrow"].ResultIU = ArrowNameToIndex("vee");
        }
        else
        {
            connector.CellsU["EndArrow"].ResultIU = ArrowNameToIndex("none");
        }

        if (attrs.TryGetValue("arrowtail", out var arrowtail))
        {
            connector.CellsU["BeginArrow"].ResultIU = ArrowNameToIndex(arrowtail);
        }
        else
        {
            connector.CellsU["BeginArrow"].ResultIU = ArrowNameToIndex("none");
        }

        // Arrow size
        if (attrs.TryGetValue("arrowsize", out var arrowsizeStr) &&
            double.TryParse(arrowsizeStr, out var arrowsize))
        {
            connector.CellsU["BeginArrowSize"].ResultIU = arrowsize;
            connector.CellsU["EndArrowSize"].ResultIU = arrowsize;
        }

        // Tooltip
        if (attrs.TryGetValue("tooltip", out var tip))
        {
            connector.CellsU["Comment"].FormulaU = $"\"{tip}\"";
        }

        // URL
        if (attrs.TryGetValue("URL", out var url))
        {
            var hyperlink = connector.Hyperlinks.Add();
            hyperlink.Address = url;
        }
    }

    public static void ApplyStyleString(Shape shape, string styleString)
    {
        if (string.IsNullOrWhiteSpace(styleString))
        {
            return;
        }

        var styles = styleString.Split(',');

        foreach (var raw in styles)
        {
            var style = raw.Trim().ToLowerInvariant();

            switch (style)
            {
                case "solid":
                    shape.CellsU["LinePattern"].ResultIU = 1;
                    break;
                case "dashed":
                    shape.CellsU["LinePattern"].ResultIU = 2;
                    break;
                case "dotted":
                    shape.CellsU["LinePattern"].ResultIU = 3;
                    break;
                case "invis":
                    shape.CellsU["LinePattern"].ResultIU = 0;
                    shape.CellsU["FillPattern"].ResultIU = 0;

                    shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowFill,
                        (short)VisCellIndices.visFillForegndTrans].FormulaForceU = "100%";
                    shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                        (short)VisCellIndices.visLineColorTrans].FormulaForceU = "100%";
                    shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowCharacter,
                        (short)VisCellIndices.visCharacterColorTrans].FormulaForceU = "100%";
                    shape.Text = "";
                    break;
                case "filled":
                    shape.CellsU["FillPattern"].ResultIU = 1;
                    break;
                case "bold":
                    shape.CellsU["LineWeight"].ResultIU = 2.0;
                    break;
                default:
                {
                    if (style.StartsWith("setlinewidth(") && style.EndsWith(")"))
                    {
                        var captures = Regex.Match(style, @"setlinewidth\(([0-9]+)\)").Groups;
                        if (captures.Count > 1)
                        {
                            var valueStr = captures[1].Value;
                            if (double.TryParse(valueStr, out var lineWidth))
                            {
                                shape.CellsU["LineWeight"].FormulaForceU = $"{lineWidth} pt";
                            }
                        }
                    }

                    break;
                }
            }
        }
    }

    private static int ArrowNameToIndex(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "none" => 0,
            "dot" => 1,
            "normal" => 2,
            "inv" => 3,
            "diamond" => 4,
            "vee" => 5,
            _ => 2 // default to normal
        };
    }
}