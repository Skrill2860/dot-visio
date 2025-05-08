using System;
using System.Collections.Generic;
using System.Globalization;
using Common;
using Domain;
using GUI.Common;
using GUI.Error_Handling;
using GUI.Gui;
using GUI.VisioConversion.DotToVisioConversionHelpers;
using Microsoft.Office.Interop.Visio;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using static System.Math;

namespace GUI.VisioConversion;

public class GraphRenderer
{
    private Window ActiveWindow;
    private double Angle;
    private double AngleStep;
    private int Biggest;
    private int Cellno; // Generate unique cell names
    private double CentreX;
    private double CentreY;
    private string ConnectorName;
    private string ConnectorStyle;
    private object ConnectorToolDataObject;
    private string ConnectTo;
    private Document CurrentDoc;
    private Page CurrentPage;
    private Layer? Layer;
    private double Length;
    private double LengthStep;
    private string MainGraphName;
    private double PageHeight;
    private Shape PageSheet;
    private double PageWidth;
    private Document Stencil;

    public void DrawGraph(Page page, Graph graph, bool newShapes, bool useVisioAutoLayout)
    {
        ProgressHelper.StartProgress("Drawing graph...", graph.ItemCount());

        ActiveWindow = SharedGui.MyVisioApp.ActiveWindow;

        // force nodes to have their name as label
        if (!graph.DefaultNodeAttributes.ContainsKey("label"))
        {
            graph.DefaultNodeAttributes.Add("label", @"\N");
        }

        MainGraphName = graph.Name;
        ConnectTo = SharedGui.CurrentDotSettings["connectto"];
        ConnectorStyle = SharedGui.CurrentDotSettings["connectorstyle"];
        ConnectorName = SharedGui.CurrentDotSettings["connectorname"];
        ConnectorToolDataObject = SharedGui.MyVisioApp.ConnectorToolDataObject;

        CurrentPage = page;

        if (newShapes)
        {
            if (CurrentPage.Shapes.Count != 0)
            {
                CurrentPage = CurrentDoc.Pages.Add();
            }

            Angle = 0d;
            AngleStep = 2d * PI / 4d;
            Biggest = 1;
            LengthStep = Biggest / 8d;
            Length = Biggest / 2d;
            // If we're drawing new shapes, we *MUST* draw new connectors as well, otherwise
            // GlueTo fails with "Inappropriate target object for this action"
            if (ConnectorStyle == "existing")
            {
                ConnectorStyle = ""; // and GetConnector will draw a spline or straight line    
            }
        }
        else
        {
            // Have to do this in two passes, delete messes up "for each"
            // (Actually if shapes was a real .NET collection, the runtime would catch the error)
            var shapes = new List<Shape>();
            foreach (Shape shp in CurrentPage.Shapes)
            {
                if (shp.GetCustomProperty("BoundingBox") == "true")
                {
                    shapes.Add(shp);
                }
            }

            foreach (var shp in shapes)
            {
                shp.Delete();
            }

            // Delete existing connectors if need be, same remarks
            if (ConnectorStyle != "existing")
            {
                var cons = new List<Shape>();
                foreach (Shape shp in CurrentPage.Shapes)
                {
                    if (shp.IsConnector())
                    {
                        cons.Add(shp);
                    }
                }

                foreach (var con in cons)
                {
                    con.Delete();
                }
            }
        }

        PageSheet = CurrentPage.PageSheet;
        PageWidth = PageSheet.Cells["PageWidth"].Result[VisUnitCodes.visInches];
        PageHeight = PageSheet.Cells["PageHeight"].Result[VisUnitCodes.visInches];

        // Connector routing style=organisation chart
        if (PageSheet.SectionExists[(short)VisSectionIndices.visSectionObject, 1] == 0)
        {
            PageSheet.AddSection((short)VisSectionIndices.visSectionObject);
        }

        PageSheet.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowPageLayout,
            (short)VisCellIndices.visPLORouteStyle].FormulaForceU = ((int)VisCellVals.visLORouteOrgChartNS).ToString();

        CentreX = PageWidth / 2d;
        CentreY = PageHeight / 2d;
        if (graph.Attributes.TryGetValue("bb", out var bbs))
        {
            if (!string.IsNullOrEmpty(bbs))
            {
                try
                {
                    var bb = BoundingBoxParser.ParseBoundingBox(bbs);
                    CentreX -= (bb.MaxX - bb.MinX) / 2d;
                    CentreY -= (bb.MaxY - bb.MinY) / 2d;
                }
                catch (DotVisioException ex)
                {
                    WarningDialogHelper.ShowWarning(ex.Message);
                }
            }
        }

        Stencil = StencilHelper.OpenStencil();

        DrawGraph(graph, newShapes);
        BringNodesToFront(graph);

        if (CurrentPage.Layers.Count == 1) // if single layer, no need for it - delete it
        {
            CurrentPage.Layers[1].Delete(0);
        }

        try
        {
            if (useVisioAutoLayout)
            {
                CurrentPage.Layout();

                UpdateSubgraphBoundingBoxes(graph);
            }

            ActiveWindow.DeselectAll();
            CurrentPage.ResizeToFitContents();
            ActiveWindow.Zoom = -1; // zoom to fit
        } catch { }

        ProgressHelper.EndProgress();
    }

    public void RedrawBoundingBoxes(Graph graph)
    {
        UpdateSubgraphBoundingBoxes(graph);
    }

    private void BringNodesToFront(Graph graph)
    {
        foreach (var sg in graph.SubGraphs)
        {
            BringNodesToFront(sg);
        }

        foreach (var node in graph.Nodes)
        {
            node.Shape?.BringToFront();
        }
    }

    private void DrawGraph(Graph graph, bool useNewShapes)
    {
        DrawAllGraphNodes(graph, useNewShapes);

        if (SharedGui.CurrentDotSettings["drawboundingboxes"].Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            DrawAllGraphBoundingBoxes(graph);
        }

        DrawAllGraphConnectors(graph);
    }

    private void DrawAllGraphNodes(Graph graph, bool useNewShapes)
    {
        foreach (var sg in graph.SubGraphs)
        {
            DrawAllGraphNodes(sg, useNewShapes);
        }

        DrawNodes(graph, useNewShapes);
    }

    private void DrawAllGraphConnectors(Graph graph)
    {
        foreach (var sg in graph.SubGraphs)
        {
            DrawAllGraphConnectors(sg);
        }

        DrawConnectors(graph);
    }

    private void DrawAllGraphBoundingBoxes(Graph graph)
    {
        foreach (var sg in graph.SubGraphs)
        {
            DrawAllGraphBoundingBoxes(sg);
        }

        if (graph.Parent != null)
        {
            DrawBoundingBox(graph);
        }
    }

    private string CalculateBoundingBox(Graph graph)
    {
        var box = graph.CalculateBoundingBox();

        box = new BoundingBox(box.MinX * 72, box.MinY * 72, box.MaxX * 72, box.MaxY * 72);

        return
            $"{box.MinX.ToString(CultureInfo.InvariantCulture)},{box.MinY.ToString(CultureInfo.InvariantCulture)},{box.MaxX.ToString(CultureInfo.InvariantCulture)},{box.MaxY.ToString(CultureInfo.InvariantCulture)}";
    }

    private void DrawBoundingBox(Graph graph)
    {
        var needsCenterOffset = true;

        if (!graph.Attributes.TryGetValue("bb", out var bbs))
        {
            bbs = CalculateBoundingBox(graph);
            graph.Attributes.Add("bb", bbs);
            needsCenterOffset = false;
        }

        SetActiveLayer(graph.Name);

        if (string.IsNullOrEmpty(bbs) || Layer is null)
        {
            return;
        }

        try
        {
            var bb = BoundingBoxParser.ParseBoundingBox(bbs);

            if (needsCenterOffset)
            {
                bb = new BoundingBox(bb.MinX + CentreX, bb.MinY + CentreY, bb.MaxX + CentreX, bb.MaxY + CentreY);
            }

            var bbox = CurrentPage.DrawRectangle(bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
            graph.Shape = bbox;

            var bgcolor = graph.GetAttribute("bgcolor");
            if (!string.IsNullOrEmpty(bgcolor))
            {
                bbox.EnsureSection((short)VisSectionIndices.visSectionObject);
                bbox.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowFill,
                    (short)VisCellIndices.visFillForegnd].FormulaForceU = ColorToVisioRgbConverter.ColorToVisioRgbFormula(bgcolor);
            }

            bbox.AddCustomProperty("IsSubgraph", "true");
            bbox.AddCustomProperty("BoundingBox", "true");
            bbox.AddCustomProperty("GraphName", graph.Name);

            graph.RenderBoundingBoxAttributes();

            Layer.Add(bbox, 0); // 0 = remove myShape from any current layers

            bbox.BringToFront();
        }
        catch (DotVisioException ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
        }
    }

    private void UpdateSubgraphBoundingBoxes(Graph graph)
    {
        foreach (var subgraph in graph.SubGraphs)
        {
            UpdateSubgraphBoundingBoxes(subgraph);
        }

        if (graph.Parent == null || graph.Shape == null)
        {
            return;
        }

        var box = graph.CalculateBoundingBox();

        var width = box.MaxX - box.MinX;
        var height = box.MaxY - box.MinY;
        var centerX = (box.MinX + box.MaxX) / 2.0;
        var centerY = (box.MinY + box.MaxY) / 2.0;

        graph.Shape.CellsU["Width"].ResultIU = width;
        graph.Shape.CellsU["Height"].ResultIU = height;
        graph.Shape.CellsU["PinX"].ResultIU = centerX;
        graph.Shape.CellsU["PinY"].ResultIU = centerY;

        // Update the "bb" attribute (in points)
        var bb = $"{(box.MinX * 72).ToString(CultureInfo.InvariantCulture)}," +
                 $"{(box.MinY * 72).ToString(CultureInfo.InvariantCulture)}," +
                 $"{(box.MaxX * 72).ToString(CultureInfo.InvariantCulture)}," +
                 $"{(box.MaxY * 72).ToString(CultureInfo.InvariantCulture)}";
        graph.Attributes["bb"] = bb;
    }

    private void DrawNodes(Graph graph, bool newShapes)
    {
        SetActiveLayer(graph.Name);

        var smartShapeWarned = false;

        foreach (var node in graph.Nodes)
        {
            try
            {
                double.TryParse(node.InheritedAttribute("width"), NumberStyles.Any, CultureInfo.InvariantCulture, out var width);
                double.TryParse(node.InheritedAttribute("height"), NumberStyles.Any, CultureInfo.InvariantCulture, out var height);

                var shape = newShapes ? CreateNewShape(node, ref width, ref height) : HandleExistingShape(node, ref smartShapeWarned);

                // Make sure myShape does have a connection-point section (that we just deleted if we're moving, not for new shapes)
                if (shape.SectionExists[(short)VisSectionIndices.visSectionConnectionPts, 1] == 0)
                {
                    shape.AddSection((short)VisSectionIndices.visSectionConnectionPts);
                }

                try
                {
                    if (width <= 0d || height <= 0d)
                    {
                        continue;
                    }

                    var shpWidth = Convert.ToDouble(shape.CellsU["width"].ResultIU, CultureInfo.InvariantCulture); // inches
                    var shpHeight = Convert.ToDouble(shape.CellsU["height"].ResultIU, CultureInfo.InvariantCulture); // inches
                    if (Abs(shpHeight / shpWidth - height / width) > 0.01d) // aspect ratio changed, adjust to new ratio
                    {
                        height = width * shpHeight / shpWidth;
                    }

                    shape.CellsU["width"].FormulaForceU = Convert.ToString(width, CultureInfo.InvariantCulture) + " IN";
                    shape.CellsU["height"].FormulaForceU = Convert.ToString(height, CultureInfo.InvariantCulture) + " IN";

                    node.Width = width;
                    node.Height = height;
                }
                catch (Exception ex)
                {
                    WarningDialogHelper.ShowWarning("Unable to set size of " + shape.Text + ": " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                WarningDialogHelper.ShowWarning("Unexpected error drawing shape " + node.Attribute("label") + ": " + ex.Message + " " +
                                                ex.StackTrace);
            }

            ProgressHelper.IncreaseProgress();
        }
    }

    private Shape CreateNewShape(Node node, ref double width, ref double height)
    {
        Shape newShape;

        var xPos = node.XPos;
        var yPos = node.YPos;

        // For graphs without position info, place shapes in a spiral from the middle
        if ((xPos == 0d) & (yPos == 0d))
        {
            Angle += AngleStep;
            Length += LengthStep;
            xPos = Length * Cos(Angle);
            yPos = Length * Sin(Angle);
            LengthStep = Biggest / 8d / Sqrt(xPos * xPos + yPos * yPos);
            AngleStep = 2d * PI / 5d * (Biggest / Sqrt(xPos * xPos + yPos * yPos));
        }

        xPos += CentreX;
        yPos += CentreY;

        var shapeName = node.InheritedAttribute("shape");
        shapeName = string.IsNullOrEmpty(shapeName) ? "ellipse" : shapeName;

        if (shapeName == "polygon")
        {
            newShape = DrawPolygon(node, ref width, ref height, xPos, yPos);
        }
        else // All shapes other than polygon (basically all shapes)
        {
            var master = GetMasterOrDefault(shapeName);
            if (master is null)
            {
                if (shapeName.StartsWith("M"))
                {
                    master = GetMasterOrDefault(shapeName.Substring(1));
                }

                if (master is null)
                {
                    WarningDialogHelper.ShowWarning("Couldn't find shape '" + shapeName + "' in Visio Stencil " +
                                                    PathUtils.ApplicationPath() +
                                                    SharedConstants.STENCILNAME + ". Using 'box'");

                    shapeName = "box";
                    master = GetMasterOrDefault(shapeName);

                    if (master is null)
                    {
                        throw new DotVisioException("Couldn't find shape 'box' in Visio Stencil " + PathUtils.ApplicationPath() +
                                                    SharedConstants.STENCILNAME);
                    }
                }
            }

            try
            {
                newShape = CurrentPage.Drop(master, xPos, yPos);
            }
            catch (Exception e)
            {
                throw new DotVisioException("Couldn't drop master '" + node.InheritedAttribute("Shape") + "' on page: " + e.Message);
            }
        }

        node.Shape = newShape;
        newShape.AddCustomProperty("Type", "Node");
        node.RenderAttributes();

        Layer?.Add(newShape, 0); // 0=remove myShape from any current layers

        return newShape;
    }

    private Shape DrawPolygon(Node node, ref double width, ref double height, double xPos, double yPos)
    {
        int.TryParse(node.InheritedAttribute("sides"), out var sides);
        if (sides < 3)
        {
            sides = 6;
        }

        if (width == 0d)
        {
            width = 1d;
        }

        if (height == 0d)
        {
            height = 1d;
        }

        var polygon = Polygon.CreatePolygon(sides + 1, width, height);
        var vertices = new double[sides * 2 + 1 + 1];
        for (int i = 0, loopTo = sides; i <= loopTo; i++)
        {
            vertices[i * 2] = polygon.Point[i].X;
            vertices[i * 2 + 1] = polygon.Point[i].Y;
        }

        var argxyArray = vertices;

        var newShape = CurrentPage.DrawPolyline(argxyArray, (short)tagVisDrawSplineFlags.visPolyline1D);
        newShape.CellsU["PinX"].FormulaForceU = $"{xPos.ToString(CultureInfo.InvariantCulture)} in";
        newShape.CellsU["PinY"].FormulaForceU = $"{yPos.ToString(CultureInfo.InvariantCulture)} in";
        return newShape;
    }

    private Shape HandleExistingShape(Node node, ref bool smartShapeWarned)
    {
        var shape = CurrentPage.Shapes.ItemFromID[Conversions.ToInteger(node.Id)];
        node.Shape = shape;
        Unprotect(node);
        var txpos = node.XPos + CentreX;
        var typos = node.YPos + CentreY;
        var pinX = Convert.ToString(txpos, CultureInfo.InvariantCulture) + " IN";
        var pinY = Convert.ToString(typos, CultureInfo.InvariantCulture) + " IN";
        try
        {
            shape.CellsU["PinX"].FormulaForceU = pinX;
            shape.CellsU["PinY"].FormulaForceU = pinY;
        }
        catch
        {
            // ignored
        }

        if (!smartShapeWarned)
        {
            if (Abs(shape.CellsU["PinX"].Result[VisUnitCodes.visInches] - txpos) > 0.01d ||
                Abs(shape.CellsU["PinY"].Result[VisUnitCodes.visInches] - typos) > 0.01d)
            {
                smartShapeWarned = true;
                WarningDialogHelper.ShowWarning("You diagram contains smart shapes that cannot be repositioned." +
                                                "Unable to change position of " + shape.Text + " from " +
                                                shape.CellsU["PinX"].Result[VisUnitCodes.visInches] + " " +
                                                shape.CellsU["PinY"].Result[VisUnitCodes.visInches] + " to " + txpos + " " +
                                                typos);
            }
        }

        if (shape.SectionExists[(short)VisSectionIndices.visSectionConnectionPts, 1] != 0)
        {
            shape.DeleteSection((short)VisSectionIndices.visSectionConnectionPts);
        }

        return shape;
    }

    private Master? GetMasterOrDefault(string name)
    {
        Stencil.Masters.GetNamesU(out var masters);
        foreach (string masterName in masters)
        {
            if (masterName == name)
            {
                return Stencil.Masters[masterName];
            }
        }

        return null;
    }

    private void DrawConnectors(Graph graph)
    {
        var layerName = graph.Name;
        SetActiveLayer(layerName);
        foreach (var edge in graph.Edges)
        {
            ProgressHelper.IncreaseProgress();
            DrawConnector(graph, edge);
        }
    }

    private Shape DrawConnector(Graph graph, Edge edge)
    {
        var fromNode = edge.FromNode;
        var toNode = edge.ToNode;
        var fromShape = fromNode.Shape;
        var toShape = toNode.Shape;
        var fromX = fromNode.XPos;
        var fromY = fromNode.YPos;
        var toX = toNode.XPos;
        var toY = toNode.YPos;

        var connector = CreateConnector(graph, edge, fromNode, toNode);
        Unprotect(edge);

        try
        {
            connector.EnsureSection((short)VisSectionIndices.visSectionObject);

            if (ConnectTo == "glue")
            {
                // make dynamic glue
                connector.CellsU["GlueType"].Formula = ((int)VisCellVals.visGlueTypeWalking).ToString();
                string pin;
                if (fromNode.XPos - toNode.XPos > fromNode.YPos - toNode.YPos) // further left/right than above/below
                {
                    pin = "PinX";
                }
                else
                {
                    pin = "PinY";
                }

                var bcell = connector.CellsU["BeginX"];
                var fromcell = fromShape.CellsU[pin];
                bcell.GlueTo(fromcell);

                var ecell = connector.CellsU["EndX"];
                var tocell = toShape.CellsU[pin];
                ecell.GlueTo(tocell);
            }
            else // Not glue, create connection points and glue to them
            {
                // Default from/to connection points to centre of from and to nodes (used for label positioning fraction below)
                var fromVisX = "=width/2";
                var toVisX = "=width/2";
                var fromVisY = "=height/2";
                var toVisY = "=height/2";

                // Relative position of from and to nodes will determine above/below or quadrants for connections
                var xoffset = toNode.XPos - fromNode.XPos;
                var yoffset = toNode.YPos - fromNode.YPos;

                Cellno++;
                var fromCellName = "c" + Cellno;
                Cellno++; // if fromShape == toShape we need a different cell name
                var toCellName = "c" + Cellno;
                connector.EnsureSection((short)VisSectionIndices.visSectionConnectionPts);

                switch (ConnectTo)
                {
                    case "centre": // the default
                    {
                        break;
                    }

                    case "topbottom":
                    {
                        if (yoffset < 0d) // tonode is in bottom quadrant
                        {
                            fromVisX = "=Width/2";
                            fromVisY = "0";
                            toVisX = "=Width/2";
                            toVisY = "=Height";
                        }
                        else // tonode is in top quadrant
                        {
                            fromVisX = "=Width/2";
                            fromVisY = "=Height";
                            toVisX = "=Width/2";
                            toVisY = "0";
                        }

                        break;
                    }

                    case "quadrant":
                    {
                        if (Abs(yoffset) < Abs(xoffset)) // tonode in left or right quadrant
                        {
                            if (xoffset >= 0d) // tonode on right, in right quadrant
                            {
                                fromVisX = "=Width";
                                fromVisY = "Height/2";
                                toVisX = "0";
                                toVisY = "=Height/2";
                            }
                            else // tonode on left, in left quadrant
                            {
                                fromVisX = "0";
                                fromVisY = "Height/2";
                                toVisX = "=Width";
                                toVisY = "=Height/2";
                            }
                        }
                        else if (yoffset < 0d) // tonode in bottom quadrant
                        {
                            fromVisX = "=Width/2";
                            fromVisY = "0";
                            toVisX = "=Width/2";
                            toVisY = "=Height";
                        }
                        else // tonode in top quadrant
                        {
                            fromVisX = "=Width/2";
                            fromVisY = "=Height";
                            toVisX = "=Width/2";
                            toVisY = "0";
                        }

                        break;
                    }

                    case "ideal":
                    {
                        if (edge.Spline.Count > 1) // Avoid problems in case of single-knotted spline (GraphViz shouldn't do that)
                        {
                            fromX = edge.Spline[0].X;
                            fromY = edge.Spline[0].Y;
                            toX = edge.Spline[edge.Spline.Count - 1].X;
                            toY = edge.Spline[edge.Spline.Count - 1].Y;
                            fromVisX = "=" + (fromX - edge.FromNode.XPos + edge.FromNode.Width / 2d);
                            fromVisY = "=" + (fromY - edge.FromNode.YPos + edge.FromNode.Height / 2d);
                            toVisX = "=" + (toX - edge.ToNode.XPos + edge.ToNode.Width / 2d);
                            toVisY = "=" + (toY - edge.ToNode.YPos + edge.ToNode.Height / 2d);
                        }

                        break;
                    }
                }

                // Create connection points in from/to nodes
                try
                {
                    var newRow = fromShape.AddNamedRow((short)VisSectionIndices.visSectionConnectionPts, fromCellName,
                        (short)VisRowTags.visTagCnnctNamed);
                    fromShape.CellsSRC[(short)VisSectionIndices.visSectionConnectionPts, newRow, (short)VisCellIndices.visX].Formula =
                        fromVisX;
                    fromShape.CellsSRC[(short)VisSectionIndices.visSectionConnectionPts, newRow, (short)VisCellIndices.visY].Formula =
                        fromVisY;

                    newRow = toShape.AddNamedRow((short)VisSectionIndices.visSectionConnectionPts, toCellName,
                        (short)VisRowTags.visTagCnnctNamed);
                    toShape.CellsSRC[(short)VisSectionIndices.visSectionConnectionPts, newRow, (short)VisCellIndices.visX].Formula =
                        toVisX;
                    toShape.CellsSRC[(short)VisSectionIndices.visSectionConnectionPts, newRow, (short)VisCellIndices.visY].Formula =
                        toVisY;

                    // Finally, glue our connector to them
                    var bcell = connector.CellsU["BeginX"];
                    var fromcell = fromShape.CellsU["connections." + fromCellName];
                    bcell.GlueTo(fromcell);

                    var ecell = connector.CellsU["EndX"];
                    var tocell = toShape.CellsU["connections." + toCellName];
                    ecell.GlueTo(tocell);
                }
                catch (Exception ex)
                {
                    throw new DotVisioException(
                        ex.Message +
                        $"FromShape='{fromShape?.Name ?? "null"}', ToShape='{toShape?.Name ?? "null"}', FromNode='{fromNode?.Id ?? "null"}', ToNode='{toNode?.Id ?? "null"}',",
                        ex);
                }
            }

            var ea = edge.GetAttribute("lp");
            if (!string.IsNullOrEmpty(ea))
            {
                try
                {
                    var lp = LabelPositionParser.ParseLabelPosition(ea);

                    var lx = lp[0] / (toX + fromX);
                    connector.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowTextXForm,
                        (short)VisCellIndices.visXFormPinX].FormulaForceU = "=Width*" + Convert.ToString(lx, CultureInfo.InvariantCulture);
                    var ly = lp[1] / (toY + fromY);
                    connector.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowTextXForm,
                        (short)VisCellIndices.visXFormPinY].FormulaForceU = "=Height*" + Convert.ToString(ly, CultureInfo.InvariantCulture);
                }
                catch (DotVisioException ex)
                {
                    WarningDialogHelper.ShowWarning(ex.Message);
                }
            }

            edge.Shape = connector;
            edge.RenderAttributes();

            Layer.Add(connector, 0); // 0=remove connector from any current layers
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning("Problem drawing connector from '" + fromNode.Attribute("label") + "' (at " + Round(fromX, 1) +
                                            "," + // TODO: here UPD: HERE WHAT??? WDYM HERE???
                                            Round(fromY, 1) + ") to '" + toNode.Attribute("label") + "' (at " + Round(toX, 1) + "," +
                                            Round(toY, 1) +
                                            "): " + ex.Message + ". Check for missing connectors. " + ex.StackTrace);
        }

        return connector;
    }

    private Shape CreateConnector(Graph graph, Edge edge, Node fromNode, Node toNode)
    {
        Shape connector;
        var fromShape = fromNode.Shape;
        var toShape = toNode.Shape;

        try
        {
            edge.Spline = SplineExtractor.ExtractSpline(edge.GetAttribute("pos"));
        }
        catch (DotVisioException ex)
        {
            WarningDialogHelper.ShowWarning(ex.Message);
            edge.Spline = [];
        }

        if (ConnectorStyle.Equals("existing", StringComparison.OrdinalIgnoreCase))
        {
            if (edge.Shape is not null)
            {
                return edge.Shape;
            }
        }

        if (ConnectorStyle != "graphviz")
        {
            try
            {
                if (string.IsNullOrEmpty(ConnectorName))
                {
                    // by default use normal connectors
                    connector = CurrentPage.Drop(ConnectorToolDataObject, CentreX, CentreY);
                    connector.CellsU["ShapeRouteStyle"].ResultIUForce = ConnectorStyle switch
                    {
                        "rightangle" => (double)VisCellVals.visLORouteRightAngle,
                        "straight" => (double)VisCellVals.visLORouteStraight,
                        _ => (double)VisCellVals.visLORouteDefault
                    };
                }
                else
                {
                    // try their connectors instead
                    var master = Stencil.Masters[ConnectorName];
                    connector = CurrentPage.Drop(master, CentreX, CentreY);
                }
            }
            catch (Exception e)
            {
                WarningDialogHelper.ShowWarning("Connector '" + ConnectorName + "' not found in Visio Stencil " +
                                                PathUtils.ApplicationPath() +
                                                SharedConstants.STENCILNAME + ": " + e.Message + ". Using straight line.");
                connector = CurrentPage.DrawLine(fromShape.CellsU["PinX"].Result[VisUnitCodes.visInches],
                    fromShape.CellsU["PinY"].Result[VisUnitCodes.visInches],
                    toShape.CellsU["PinX"].Result[VisUnitCodes.visInches],
                    toShape.CellsU["PinY"].Result[VisUnitCodes.visInches]);
            }
        }
        else
        {
            if (edge.Spline.Count < 2) // Strange. Draw between centres
            {
                connector = CurrentPage.DrawLine(fromShape.CellsU["PinX"].Result[VisUnitCodes.visInches],
                    fromShape.CellsU["PinY"].Result[VisUnitCodes.visInches],
                    toShape.CellsU["PinX"].Result[VisUnitCodes.visInches],
                    toShape.CellsU["PinY"].Result[VisUnitCodes.visInches]);
            }
            else if (ConnectorStyle == "straight" || edge.Spline.Count == 2) // Straight line, express or implied
            {
                connector = CurrentPage.DrawLine(CentreX + edge.Spline[0].X, CentreY + edge.Spline[0].Y,
                    CentreX + edge.Spline[edge.Spline.Count - 1].X, CentreY + edge.Spline[edge.Spline.Count - 1].Y);
            }
            else // real spline
            {
                var controlpoints = edge.Spline.Count;
                var degree = 6;
                if (degree >= controlpoints)
                {
                    degree = controlpoints - 1;
                }

                var knots = controlpoints + 1;
                var knot = new double[controlpoints + 1];
                var j = 0;
                for (int i = 0, loopTo = knots - 1; i <= loopTo; i++)
                {
                    if (i <= degree)
                    {
                        knot[i] = 0d;
                    }
                    else
                    {
                        j = j + 1;
                        knot[i] = j;
                    }
                }

                var xy = new double[controlpoints * 2];
                for (int i = 0, loopTo1 = edge.Spline.Count - 1; i <= loopTo1; i++)
                {
                    xy[i * 2] = edge.Spline[i].X;
                    xy[i * 2 + 1] = edge.Spline[i].Y;
                }

                Array argxyArray = xy;
                Array argknots = knot;
                connector = CurrentPage.DrawNURBS((short)degree, (short)tagVisDrawSplineFlags.visSpline1D, ref argxyArray, ref argknots);
            }
        }

        return connector;
    }

    private void SetActiveLayer(string layerName)
    {
        Layer = null;

        if (layerName.Equals("Connector", StringComparison.InvariantCultureIgnoreCase))
        {
            layerName = MainGraphName;
        }

        for (int i = 1, loopTo = CurrentPage.Layers.Count; i <= loopTo; i++)
        {
            if (CurrentPage.Layers[i].Name == layerName)
            {
                Layer = CurrentPage.Layers[i];
                break;
            }
        }

        Layer ??= CurrentPage.Layers.Add(layerName);
    }

    private void Unprotect(Node node)
    {
        if (node.Shape is null)
        {
            return;
        }

        try
        {
            var shape = node.Shape;
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockAspect].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockBegin].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockCalcWH].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockCrop].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockDelete].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockEnd].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockFormat].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockGroup].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockHeight].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockMoveX].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockMoveY].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockRotate].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockSelect].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockTextEdit].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockVtxEdit].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockWidth].FormulaForceU = "0";
        }
        catch (Exception ex)
        {
            WarningDialogHelper.ShowWarning("Error unprotecting shape " + node.Attribute("label") + ":" + Constants.vbCrLf + ex.Message);
        }
    }

    private void Unprotect(Edge edge)
    {
        if (edge.Shape is null)
        {
            return;
        }

        try
        {
            var shape = edge.Shape;

            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockAspect].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockBegin].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockCalcWH].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockCrop].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockDelete].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock, (short)VisCellIndices.visLockEnd]
                .FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockFormat].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockGroup].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockHeight].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockMoveX].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockMoveY].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockRotate].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockSelect].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockTextEdit].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockVtxEdit].FormulaForceU = "0";
            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLock,
                (short)VisCellIndices.visLockWidth].FormulaForceU = "0";
        }
        catch (Exception ex)
        {
            var desc = "connector";

            try
            {
                desc = desc + " from " + (edge.FromNode?.Attribute("label") ?? "FromNodeNULL");
            }
            catch (Exception ex1)
            {
                desc = desc + " from [" + ex1.Message + "?]";
            }

            try
            {
                desc = desc + " to " + (edge.ToNode?.Attribute("label") ?? "ToNodeNULL");
            }
            catch (Exception ex2)
            {
                desc = desc + "to [" + ex2.Message + "?]";
            }

            WarningDialogHelper.ShowWarning("Error while unprotecting " + desc + ":" + Constants.vbCrLf + ex.Message);
        }
    }
}