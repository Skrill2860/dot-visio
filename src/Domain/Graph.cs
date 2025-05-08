using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common;
using Microsoft.Office.Interop.Visio;

namespace Domain;

public class Graph(string name)
{
    private bool _isDigraph;

    public bool Strict = false;

    public Dictionary<string, string> Attributes = new();
    public Dictionary<string, string> DefaultNodeAttributes = new();
    public Dictionary<string, string> DefaultEdgeAttributes = new();

    public Dictionary<string, Node> NodeNames = new();

    public readonly List<Node> Nodes = [];
    public Dictionary<string, Node> GlobalNodeNames = new();
    public readonly List<Edge> Edges = [];
    public Dictionary<string, Edge> GlobalEdgeNames = new();
    public readonly List<Graph> SubGraphs = [];

    public string Name = name;
    public Graph? Parent;
    public Shape? Shape;

    public bool IsDigraph
    {
        get
        {
            if (Parent is null)
            {
                return _isDigraph;
            }

            return Parent.IsDigraph;
        }
        set
        {
            if (Parent is null)
            {
                _isDigraph = value;
            }
            else
            {
                Parent.IsDigraph = value;
            }
        }
    }

    public Node AddNode(string nodeName)
    {
        if (GlobalNodeNames.TryGetValue(nodeName, out var node))
        {
            return node;
        }

        node = new Node(nodeName);
        AddNode(node);
        return node;
    }

    public void AddNode(Node node)
    {
        if (!GlobalNodeNames.ContainsKey(node.Id))
        {
            GlobalNodeNames.Add(node.Id, node);
        }

        if (!NodeNames.ContainsKey(node.Id))
        {
            NodeNames.Add(node.Id, node);
            Nodes.Add(node);
        }

        node.Graph = this;
    }

    public void AddEdge(Edge edge)
    {
        if (Strict &&
            Edges.Any(e => e.FromNode?.Id == edge.FromNode?.Id && e.ToNode?.Id == edge.ToNode?.Id))
        {
            return;
        }

        if (edge.FromNode.Graph is null)
        {
            edge.FromNode.Graph = this;
            if (!NodeNames.ContainsKey(edge.FromNode.Id))
            {
                NodeNames.Add(edge.FromNode.Id, edge.FromNode);
                Nodes.Add(edge.FromNode);
            }
        }

        if (edge.ToNode.Graph is null)
        {
            edge.ToNode.Graph = this;
            if (!NodeNames.ContainsKey(edge.ToNode.Id))
            {
                NodeNames.Add(edge.ToNode.Id, edge.ToNode);
                Nodes.Add(edge.ToNode);
            }
        }

        edge.Graph = this;
        Edges.Add(edge);
        if (!GlobalEdgeNames.ContainsKey(edge.Id))
        {
            GlobalEdgeNames.Add(edge.Id, edge);
        }

        if (!edge.FromNode.Edges.Contains(edge))
        {
            edge.FromNode.Edges.Add(edge);
        }

        if (!edge.ToNode.Edges.Contains(edge))
        {
            edge.ToNode.Edges.Add(edge);
        }
    }

    public void RemoveEdge(Edge Edge)
    {
        if (GlobalEdgeNames.ContainsKey(Edge.Id))
        {
            GlobalEdgeNames.Remove(Edge.Id);
        }

        Edges.Remove(Edge);
    }

    public void RemoveNode(Node node)
    {
        if (GlobalNodeNames.ContainsKey(node.Id))
        {
            GlobalNodeNames.Remove(node.Id);
        }

        if (NodeNames.ContainsKey(node.Id))
        {
            NodeNames.Remove(node.Id);
        }

        Nodes.Remove(node);
    }

    public void AddSubGraph(Graph SubGraph)
    {
        foreach (var node in SubGraph.Nodes)
        {
            if (!GlobalNodeNames.ContainsKey(node.Id))
            {
                GlobalNodeNames.Add(node.Id, node);
            }
        }

        foreach (var edge in SubGraph.Edges)
        {
            if (!GlobalEdgeNames.ContainsKey(edge.Id))
            {
                GlobalEdgeNames.Add(edge.Id, edge);
            }
        }

        SubGraph.GlobalNodeNames = GlobalNodeNames;
        SubGraph.GlobalEdgeNames = GlobalEdgeNames;
        SubGraph.Parent = this;
        SubGraphs.Add(SubGraph);
    }

    public Edge Connect(Node FromNode, Node ToNode)
    {
        var Edge = new Edge();
        Edge.FromNode = FromNode;
        Edge.ToNode = ToNode;
        AddEdge(Edge);
        return Edge;
    }

    public void CopyLayoutFrom(Graph srcGraph)
    {
        SetAttribute("bb", srcGraph.GetAttribute("bb"));

        foreach (var node in GlobalNodeNames.Values)
        {
            if (srcGraph.GlobalNodeNames.TryGetValue(node.Id, out var newNode))
            {
                node.SetAttribute("pos", newNode.Attribute("pos"));
            }
        }

        foreach (var edge in GlobalEdgeNames.Values)
        {
            if (srcGraph.GlobalEdgeNames.TryGetValue(edge.Id, out var newEdge))
            {
                edge.SetAttribute("pos", newEdge.GetAttribute("pos"));
            }
        }
    }

    // Find a node from its name, adding it to the graph's global list but to any particular (sub)graph
    // Used principally by DotParser
    public Node FindNode(string id)
    {
        if (!GlobalNodeNames.TryGetValue(id, out var node))
        {
            node = new Node(id);
            GlobalNodeNames.Add(id, node);
        }

        return node;
    }

    public string GetAttribute(string key)
    {
        Attributes.TryGetValue(key, out var s);
        return s ?? "";
    }

    public int ItemCount()
    {
        return GlobalNodeNames.Count + GlobalEdgeNames.Count;
    }

    private int ItemsIn(Graph graph)
    {
        var ans = 0;
        foreach (var sg in graph.SubGraphs)
        {
            ans += ItemsIn(sg);
        }

        return Nodes.Count + Edges.Count + ans;
    }

    public bool NodeExists(string ID)
    {
        return GlobalNodeNames.ContainsKey(ID);
    }

    private void RenameGraphNodes(Graph graph)
    {
        foreach (var sg in graph.SubGraphs)
        {
            RenameGraphNodes(sg);
        }

        var newNodes = new Dictionary<string, Node>();
        foreach (var node in graph.Nodes)
        {
            newNodes.Add(node.NewName, node);
            node.Id = node.NewName;
        }

        graph.NodeNames = newNodes;
    }

    public void RenameNodes()
    {
        var newNodes = new Dictionary<string, Node>();
        var allNodes = GlobalNodeNames.Values;
        foreach (var node in allNodes)
        {
            var nodeLabel = node.Attribute("label");

            if (string.IsNullOrEmpty(nodeLabel))
            {
                nodeLabel = node.Id;
            }

            var unique = 0;
            var newLabel = nodeLabel;
            while (newNodes.ContainsKey(newLabel))
            {
                unique = unique + 1;
                newLabel = nodeLabel + unique;
            }

            newNodes.Add(newLabel, node);
            node.NewName = newLabel;
        }

        GlobalNodeNames = newNodes;

        RenameGraphNodes(this);
    }

    public void SetAttribute(string key, string value)
    {
        Attributes[key] = value;
    }

    public Graph GetOrCreateSubGraph(string subGraphName)
    {
        if (subGraphName == Name)
        {
            return this;
        }

        if (string.IsNullOrEmpty(subGraphName))
        {
            subGraphName = UniqueNameGenerator.GenerateUniqueName("cluster_");
        }
        else
        {
            foreach (var sg in SubGraphs)
            {
                if (sg.Name == subGraphName)
                {
                    return sg;
                }
            }
        }

        var newSubGraph = new Graph(subGraphName);
        AddSubGraph(newSubGraph);

        newSubGraph.GlobalNodeNames = GlobalNodeNames;
        newSubGraph.Parent = this;
        return newSubGraph;
    }

    public BoundingBox CalculateBoundingBox()
    {
        double minX, minY, maxX, maxY;
        minX = minY = double.MaxValue;
        maxX = maxY = double.MinValue;

        const double offsetInches = 0.1;
        var offsetInchesX = offsetInches;
        var offsetInchesY = offsetInches;

        foreach (var sg in SubGraphs)
        {
            var sgCoords = sg.CalculateBoundingBox();
            minX = Math.Min(minX, sgCoords.MinX);
            minY = Math.Min(minY, sgCoords.MinY);
            maxX = Math.Max(maxX, sgCoords.MaxX);
            maxY = Math.Max(maxY, sgCoords.MaxY);
        }

        foreach (var node in Nodes)
        {
            if (node.Shape is null)
            {
                continue;
            }

            minX = Math.Min(node.Shape.CellsU["PinX"].ResultIU - node.Shape.CellsU["width"].ResultIU / 2, minX);
            minY = Math.Min(node.Shape.CellsU["PinY"].ResultIU - node.Shape.CellsU["height"].ResultIU / 2, minY);
            maxX = Math.Max(node.Shape.CellsU["PinX"].ResultIU + node.Shape.CellsU["width"].ResultIU / 2, maxX);
            maxY = Math.Max(node.Shape.CellsU["PinY"].ResultIU + node.Shape.CellsU["height"].ResultIU / 2, maxY);
        }

        return new BoundingBox(minX - offsetInchesX, minY - offsetInchesY, maxX + offsetInchesX, maxY + offsetInchesY);
    }
}