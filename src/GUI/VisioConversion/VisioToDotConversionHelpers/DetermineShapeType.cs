using System;
using System.Globalization;
using Domain;
using Microsoft.Office.Interop.Visio;
using static System.Math;

namespace GUI.VisioConversion.VisioToDotConversionHelpers;

public static class ShapeTypeHelper
{
    public static void DetermineShapeType(Node node)
    {
        if (node.Shape is null)
        {
            node.SetAttribute("shape", "box");

            return;
        }

        var curves = 0;
        var lines = 0;
        var corners = 0;
        var topcorners = 0;
        var bottomcorners = 0;
        var midpoints = 0;
        var shape = node.Shape;
        var shapewidth = shape.CellsU["width"].Result[VisUnitCodes.visInches];
        var shapeheight = shape.CellsU["height"].Result[VisUnitCodes.visInches];
        var type = "box";
        int gsects = shape.GeometryCount;
        double lastXMove = -1;
        double lastYMove = -1;

        for (int gsect = 0, loopTo = gsects - 1; gsect <= loopTo; gsect++) // examine geometry
        {
            var cgs = (short)((int)VisSectionIndices.visSectionFirstComponent + gsect);
            var rows = shape.RowCount[cgs];
            for (short gRow = 0, loopTo1 = (short)(rows - 1); gRow <= loopTo1; gRow++)
            {
                int rowtype = shape.RowType[cgs, gRow];
                switch (rowtype)
                {
                    case (int)VisRowTags.visTagEllipse:
                    {
                        curves = curves + 1;
                        break;
                    }
                    case (int)VisRowTags.visTagEllipticalArcTo:
                    {
                        curves = curves + 1;
                        break;
                    }
                    case (int)VisRowTags.visTagLineTo:
                    {
                        lines = lines + 1;
                        var X = shape.CellsSRC[cgs, gRow, (short)VisCellIndices.visX].Result[VisUnitCodes.visInches];
                        var Y = shape.CellsSRC[cgs, gRow, (short)VisCellIndices.visY].Result[VisUnitCodes.visInches];
                        if (Close(X, 0d) | Close(X, shapewidth))
                        {
                            if (Close(Y, 0d))
                            {
                                corners = corners + 1;
                                bottomcorners = bottomcorners + 1;
                            }
                            else if (Close(Y, shapeheight))
                            {
                                corners = corners + 1;
                                topcorners = topcorners + 1;
                            }
                            else if (Close(Y, shapeheight / 2d))
                            {
                                midpoints = midpoints + 1;
                            }
                        }
                        else if (Close(X, shapewidth / 2d))
                        {
                            if (Close(Y, 0d) | Close(Y, shapeheight))
                            {
                                midpoints = midpoints + 1;
                            }
                        }

                        break;
                    }
                    case (int)VisRowTags.visTagInfiniteLine:
                    {
                        break;
                    }
                    case (int)VisRowTags.visTagNURBSTo:
                    {
                        break;
                    }
                    case (int)VisRowTags.visTagMoveTo:
                    {
                        lastXMove = shape.CellsSRC[cgs, gRow, (short)VisCellIndices.visX].Result[VisUnitCodes.visInches] /
                                    shapewidth;
                        lastYMove = shape.CellsSRC[cgs, gRow, (short)VisCellIndices.visY].Result[VisUnitCodes.visInches] /
                                    shapeheight;
                        break;
                    }
                    case (int)VisRowTags.visTagPolylineTo:
                    {
                        var polyline = shape.CellsSRC[cgs, gRow, (short)VisCellIndices.visPolylineData].FormulaU;
                        var lp = polyline.IndexOf("(", StringComparison.Ordinal);
                        var rp = polyline.IndexOf(")", lp, StringComparison.Ordinal);
                        if ((lp >= 0) & (rp >= 0))
                        {
                            polyline = polyline.Substring(lp + 1, rp - lp - 1);
                        }

                        string[] points = polyline.Split(',');
                        points[0] = lastXMove.ToString(CultureInfo.InvariantCulture);
                        points[1] = lastYMove.ToString(CultureInfo.InvariantCulture);
                        lines = lines + (int)Round((points.GetUpperBound(0) + 1) / 2d);
                        for (int i = 0, loopTo2 = points.GetUpperBound(0) - 1; i <= loopTo2; i += 2)
                        {
                            var x = Convert.ToDouble(points[i], CultureInfo.InvariantCulture);
                            var y = Convert.ToDouble(points[i + 1], CultureInfo.InvariantCulture);
                            if (Close(x, 0d) || Close(x, 1d))
                            {
                                if (Close(y, 0d))
                                {
                                    corners = corners + 1;
                                    bottomcorners = bottomcorners + 1;
                                }
                                else if (Close(y, 1d))
                                {
                                    corners = corners + 1;
                                    topcorners = topcorners + 1;
                                }
                                else if (Close(y, 0.5d))
                                {
                                    midpoints = midpoints + 1;
                                }
                            }
                            else if (Close(x, 0.5d))
                            {
                                if (Close(y, 0d) | Close(y, 1d))
                                {
                                    midpoints = midpoints + 1;
                                }
                            }
                        }

                        break;
                    }
                    case (int)VisRowTags.visTagSplineBeg:
                    {
                        break;
                    }
                    case (int)VisRowTags.visTagSplineSpan:
                    {
                        break;
                    }
                }
            }
        }

        if (curves > 0)
        {
            if (shapewidth == shapeheight)
            {
                type = "circle";
            }
            else
            {
                type = "ellipse";
            }
        }
        else
        {
            switch (lines)
            {
                case 3:
                {
                    type = "triangle";
                    break;
                }
                case 4:
                {
                    if (corners == 4)
                    {
                        type = "box";
                    }
                    else if (midpoints == 4)
                    {
                        type = "diamond";
                    }
                    else if (bottomcorners == 2)
                    {
                        type = "trapezium";
                    }
                    else
                    {
                        type = "parallelogram";
                    }

                    break;
                }

                default:
                {
                    if (lines == 0)
                    {
                        type = "box";
                    }
                    else
                    {
                        type = "polygon";
                        node.SetAttribute("sides", lines.ToString());
                    }

                    break;
                }
            }
        }

        node.SetAttribute("shape", type);
    }

    private static bool Close(double a, double b)
    {
        return Abs(a - b) < 0.01d;
    }
}