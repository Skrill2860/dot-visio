using System.Collections.Generic;
using System.Globalization;
using Domain;

namespace DotCore.DOT;

public partial class DotWriter
{
    public static void MergeAttributes(Graph graph)
    {
        MergeAttributesCore(graph);
        foreach (var sg in graph.SubGraphs)
        {
            MergeAttributesCore(sg);
        }
    }

    private static void MergeAttributesCore(Graph graph)
    {
        bool all;

        // Merge node attributes
        Dictionary<string, string> commonAttributes = new();
        Dictionary<string, string> notCommonAttributes = new();

        foreach (var node in graph.Nodes)
        {
            foreach (var pair in node.Attributes)
            {
                if (commonAttributes.TryGetValue(pair.Key, out var value))
                {
                    if (pair.Value == value || notCommonAttributes.ContainsKey(pair.Key))
                    {
                        continue;
                    }

                    notCommonAttributes.Add(pair.Key, pair.Value);
                }
                else
                {
                    commonAttributes.Add(pair.Key, pair.Value);
                }
            }
        }

        foreach (var pair in notCommonAttributes)
        {
            commonAttributes.Remove(pair.Key);
        }

        foreach (var pair in commonAttributes)
        {
            all = true;
            foreach (var node in graph.Nodes)
            {
                if (!node.Attributes.ContainsKey(pair.Key))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                graph.DefaultNodeAttributes.Add(pair.Key, pair.Value);
                foreach (var node in graph.Nodes)
                {
                    node.Attributes.Remove(pair.Key);
                }
            }
        }

        // Merge edge attributes
        commonAttributes = new Dictionary<string, string>();
        notCommonAttributes = new Dictionary<string, string>();
        foreach (var edge in graph.Edges)
        {
            foreach (var pair in edge.Attributes)
            {
                if (commonAttributes.ContainsKey(pair.Key))
                {
                    if (pair.Value != commonAttributes[pair.Key])
                    {
                        if (!notCommonAttributes.ContainsKey(pair.Key))
                        {
                            notCommonAttributes.Add(pair.Key, pair.Value);
                        }
                    }
                }
                else
                {
                    commonAttributes.Add(pair.Key, pair.Value);
                }
            }
        }

        foreach (KeyValuePair<string, string> pair in notCommonAttributes)
        {
            commonAttributes.Remove(pair.Key);
        }

        foreach (KeyValuePair<string, string> pair in commonAttributes)
        {
            all = true;
            foreach (var edge in graph.Edges)
            {
                if (!edge.Attributes.ContainsKey(pair.Key))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                graph.DefaultEdgeAttributes.Add(pair.Key, pair.Value);
                foreach (var edge in graph.Edges)
                {
                    edge.Attributes.Remove(pair.Key);
                }
            }
        }
    }
}