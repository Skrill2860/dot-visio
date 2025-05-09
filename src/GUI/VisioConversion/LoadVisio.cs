using System;
using System.Collections.Generic;
using System.Globalization;
using Common;
using Domain;
using GUI.Error_Handling;
using GUI.Gui;
using GUI.VisioConversion.VisioToDotConversionHelpers;
using Microsoft.Office.Interop.Visio;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GUI.VisioConversion;

public static class LoadVisio
{
    public static Graph LoadGraphFromVisioPage(IVPage page)
    {
        var connections = new Dictionary<Connection, bool>();

        var graph = new Graph(SharedConstants.ROOTGRAPHNAME);

        ProgressHelper.StartProgress("Analysing diagram...", page.Shapes.Count);

        foreach (Shape shape in page.Shapes)
        {
            ProgressHelper.IncreaseProgress();

            if (shape.CellExistsU["BeginX", Conversions.ToShort(true)] != 0) // a 1D shape, a connector
            {
                if (shape.Connects.Count == 2) // Connector between 2 shapes
                {
                    var conn = new Connection(shape.Connects[1].ToCell.Shape.ID, shape.Connects[2].ToCell.Shape.ID);
                    if (connections.ContainsKey(conn))
                    {
                        continue;
                    }

                    connections.Add(conn, true);

                    try
                    {
                        // If either end of the connector is an arrow, this is a digraph (directed graph)
                        if (shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                                (short)VisCellIndices.visLineEndArrow].ResultIU != 0d ||
                            shape.CellsSRC[(short)VisSectionIndices.visSectionObject, (short)VisRowIndices.visRowLine,
                                (short)VisCellIndices.visLineBeginArrow].ResultIU != 0d)
                        {
                            graph.IsDigraph = true;
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    var subgraph = graph;

                    var fromShape = shape.Connects[1].ToCell.Shape;
                    var toShape = shape.Connects[2].ToCell.Shape;

                    var fromNode = FindOrCreateShape(graph, fromShape);
                    var toNode = FindOrCreateShape(graph, toShape);

                    var edge = subgraph.Connect(fromNode, toNode);
                    edge.Shape = shape;
                    edge.Attributes = VisioToDotEdgeMapper.ExtractDotAttributes(shape);

                    var sText = shape.Text;
                    SetShapeText(edge, ref sText);
                }
            }
            else if (shape.GetCustomProperty("BoundingBox") == "true") // A bounding box
            {
                var thisnode = FindOrCreateShape(graph, shape);
                var graphname = shape.GetCustomProperty("GraphName");
                var subgraph = graph.GetOrCreateSubGraph(graphname);

                foreach (var attr in thisnode.Attributes.Keys)
                {
                    subgraph.Attributes.Add(attr, thisnode.Attributes[attr]);
                }

                subgraph.Shape = shape;
                subgraph.RemoveNode(thisnode);
            }
            else // a 2-D shape.
            {
                var thisnode = FindOrCreateShape(graph, shape);
            }
        }

        ProgressHelper.EndProgress();

        return graph;
    }

    private static void SetShapeText(object thing, ref string text)
    {
        var shapetext = text;
        if (!string.IsNullOrEmpty(shapetext))
        {
            for (int i = 0, loopTo = shapetext.Length - 1; i <= loopTo; i++)
            {
                var ch = Strings.AscW(shapetext.Substring(i, 1));
                if ((ch < 32) | (ch > 255))
                {
                    shapetext = shapetext.Replace(Conversions.ToString(Strings.ChrW(ch)), " ");
                }
            }

            if (!string.IsNullOrEmpty(shapetext))
            {
                ((dynamic)thing).SetAttribute("label", shapetext);
            }
        }
    }

    private static Node FindOrCreateShape(Graph graph, Shape shape)
    {
        // Find node in graph or any subgraph
        if (graph.NodeNames.TryGetValue(shape.ID.ToString(), out var node))
        {
            return node;
        }

        foreach (var sg in graph.SubGraphs)
        {
            if (sg.NodeNames.TryGetValue(shape.ID.ToString(), out node))
            {
                return node;
            }
        }

        if (shape.LayerCount > 0 && !shape.Layer[1].Name.Equals(SharedConstants.ROOTGRAPHNAME, StringComparison.OrdinalIgnoreCase))
        {
            var subGraphName = shape.Layer[1].Name;
            var subgraph = graph.GetOrCreateSubGraph(subGraphName);

            node = subgraph.AddNode(shape.ID.ToString());

            if (shape.LayerCount > 1)
            {
                WarningDialogHelper.ShowWarning("Shape " + shape.Text + " is in more than 1 layer, it may appear repeatedly after layout");
            }
        }
        else
        {
            node = graph.AddNode(shape.ID.ToString());
        }

        node.Shape = shape;

        node.Attributes = VisioToDotNodeMapper.ExtractDotAttributes(shape);

        var sText = shape.Text;
        SetShapeText(node, ref sText);
        if (node.Attributes.ContainsKey("shape") && node.Attributes["shape"] == "box")
        {
            ShapeTypeHelper.DetermineShapeType(node);
        }

        return node;
    }

    private record Connection
    {
        public int FromNode;
        public int ToNode;

        public Connection(int f, int t)
        {
            FromNode = f;
            ToNode = t;
        }
    }
}