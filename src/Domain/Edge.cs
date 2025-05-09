using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Microsoft.Office.Interop.Visio;

namespace Domain;

public record Edge
{
    public string Id;
    
    public Graph Graph = null!;
    public Node? FromNode = null;
    public Node? ToNode = null;
    public Shape? Shape = null;
    
    public Dictionary<string, string> Attributes = new();
    public List<Coordinate> Spline = [];

    public Edge()
    {
        Id = UniqueNameGenerator.GenerateUniqueName();
    }

    public string GetAttribute(string key)
    {
        if (Attributes.TryGetValue(key, out var s))
        {
            return s;
        }

        return "";
    }

    public void SetAttribute(string key, string value)
    {
        if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            Id = value;
        }
        else
        {
            Attributes[key] = value;
        }
    }

    public void CoalesceInheritedAttributes()
    {
        Dictionary<string, string> coalescedAttributes = new();

        foreach (var attr in Attributes)
        {
            coalescedAttributes[attr.Key] = attr.Value;
        }

        var pg = Graph;
        while (pg is not null)
        {
            foreach (var attr in pg.DefaultEdgeAttributes)
            {
                if (!coalescedAttributes.ContainsKey(attr.Key))
                {
                    coalescedAttributes[attr.Key] = attr.Value;
                }
            }

            pg = pg.Parent;
        }

        Attributes = coalescedAttributes;
    }

    public override string ToString()
    {
        return
            $"Edge: from '{FromNode.Id}' to '{ToNode.Id}', Attributes=[{string.Join("|", Attributes.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]";
    }
}