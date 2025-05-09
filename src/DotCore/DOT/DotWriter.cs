using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Domain;

namespace DotCore.DOT;

public partial class DotWriter
{
    public enum DotOutputDetalizationLevels
    {
        Full,
        Minimal
    }

    private StreamWriter _ofile;
    private DotOutputDetalizationLevels _detalizationLevel;
    private string _arrow = "";

    public event Action WriteProgressIncreaseEvent = delegate { };

    public void WriteDot(Graph graph, string filename, DotOutputDetalizationLevels dotOutputDetalizationLevel = DotOutputDetalizationLevels.Full)
    {
        _detalizationLevel = dotOutputDetalizationLevel;

        var utf8WithoutBom = new UTF8Encoding(false);

        _ofile = new StreamWriter(filename, false, utf8WithoutBom);

        if (graph.Strict)
        {
            _ofile.WriteLine("strict ");
        }

        if (graph.IsDigraph)
        {
            _ofile.WriteLine("digraph " + graph.Name + " {");
            _arrow = "->";
        }
        else
        {
            _ofile.WriteLine("graph " + graph.Name + " {");
            _arrow = "--";
        }

        MergeAttributes(graph);

        WriteGraph(graph, 0);
        _ofile.WriteLine("}");
        _ofile.WriteLine("// Exported using DotVisio Extension");
        _ofile.Close();
    }

    private void WriteGraph(Graph graph, int indent)
    {
        if (graph.Attributes.Count > 0)
        {
            _ofile.Write(string.Empty.PadLeft(indent + 2, ' ') + "graph [");

            WriteAttributes(_ofile, graph.Attributes);

            _ofile.WriteLine("];");
        }

        if (graph.DefaultNodeAttributes.Count > 0)
        {
            _ofile.Write(string.Empty.PadLeft(indent + 2, ' ') + "node [");
            
            WriteAttributes(_ofile, graph.DefaultNodeAttributes);

            _ofile.WriteLine("];");
        }

        if (graph.DefaultEdgeAttributes.Count > 0)
        {
            _ofile.Write(string.Empty.PadLeft(indent + 2, ' ') + "edge [");
            
            WriteAttributes(_ofile, graph.DefaultEdgeAttributes);

            _ofile.WriteLine("];");
        }

        foreach (var sg in graph.SubGraphs)
        {
            _ofile.WriteLine(string.Empty.PadLeft(indent + 2, ' ') + "subgraph " + sg.Name + " {");
            WriteGraph(sg, indent + 2);
            _ofile.WriteLine(string.Empty.PadLeft(indent + 2, ' ') + "}");
        }

        foreach (var node in graph.Nodes)
        {
            WriteNode(node, indent);

            WriteProgressIncreaseEvent.Invoke();
        }

        foreach (var edge in graph.Edges)
        {
            WriteEdge(edge, indent);

            WriteProgressIncreaseEvent.Invoke();
        }
    }

    private void WriteNode(Node node, int indent)
    {
        _ofile.Write(string.Empty.PadLeft(indent + 2, ' ') + node.Id.Quote() + " [");

        if (_detalizationLevel == DotOutputDetalizationLevels.Full)
        {
            WriteAttributes(_ofile, node.Attributes);
        }
        else if (_detalizationLevel == DotOutputDetalizationLevels.Minimal)
        {
            var allowedMinimalAttributes = new[] { "width", "height", "shape", "sides" };

            var minimalAttributes = node.Attributes
                .Where(key => allowedMinimalAttributes.Contains(key.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            WriteAttributes(_ofile, minimalAttributes);
        }

        _ofile.WriteLine("];");
    }

    private void WriteEdge(Edge edge, int indent)
    {
        _ofile.Write($"{"".PadLeft(indent + 2, ' ')}{edge.FromNode?.Id.Quote() ?? ""}{_arrow}{edge.ToNode?.Id.Quote() ?? ""}");

        if (edge.Attributes.Count == 0)
        {
            _ofile.WriteLine(";");
            
            return;
        }

        _ofile.Write(" [");
        if (_detalizationLevel == DotOutputDetalizationLevels.Minimal)
        {
            _ofile.Write("id=" + edge.Id.Quote());
            if (edge.Attributes.TryGetValue("constraint", out var constraintAttribute))
            {
                _ofile.Write($"constraint={constraintAttribute}");
            }
        }
        else
        {
            var attributes = edge.Attributes
                .Where(kvp => SharedConstants.GraphvizGraphOptions.Contains(kvp.Key))
                .Select(kvp => $"{kvp.Key}={kvp.Value.QuoteIf()}")
                .ToArray();
            _ofile.Write(string.Join(", ", attributes));
        }

        _ofile.WriteLine("];");
    }

    private void WriteAttributes(StreamWriter writer, Dictionary<string, string> attributes)
    {
        var attributesStrings = attributes
            .Where(kvp => SharedConstants.GraphvizGraphOptions.Contains(kvp.Key))
            .Select(kvp => $"{kvp.Key}={kvp.Value.QuoteIf()}")
            .ToArray();

        writer.Write(string.Join(", ", attributesStrings));
    }
}