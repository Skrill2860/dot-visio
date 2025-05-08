using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common;
using Microsoft.Office.Interop.Visio;

namespace Domain;

public record Node
{
    public readonly List<Edge> Edges = [];
    public Dictionary<string, string> Attributes = new();

    public Graph Graph = null!;

    public double Height;

    public string Id;
    public string NewName = "";

    public Shape? Shape = null;
    public double Width;
    public double XPos;
    public double YPos;

    public Node(string newid)
    {
        Id = newid;
    }

    public string Attribute(string key)
    {
        Attributes.TryGetValue(key, out var s);
        return s ?? "";
    }

    public string InheritedAttribute(string key)
    {
        if (Attributes.TryGetValue(key, out var s))
        {
            return s;
        }

        var pg = Graph;
        while (pg is not null)
        {
            if (pg.DefaultNodeAttributes.TryGetValue(key, out s))
            {
                return s;
            }

            pg = pg.Parent;
        }

        return "";
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
            foreach (var attr in pg.DefaultNodeAttributes)
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

    public void SetAttribute(string key, string value)
    {
        Attributes[key] = value;

        switch (key ?? "")
        {
            case var @case when CultureInfo.CurrentCulture.CompareInfo.Compare(@case, "pos",
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value = value.CleanupPos();
                    try
                    {
                        var xy = value.Split(',');
                        XPos = Convert.ToDouble(xy[0], CultureInfo.InvariantCulture) / 72d;
                        YPos = Convert.ToDouble(xy[1].Replace("!", ""), CultureInfo.InvariantCulture) / 72d;
                    }
                    catch
                    {
                    }
                }

                break;
            }
            case var case1 when CultureInfo.CurrentCulture.CompareInfo.Compare(case1, "width",
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
            {
                Width = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            }
            case var case2 when CultureInfo.CurrentCulture.CompareInfo.Compare(case2, "height",
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
            {
                Height = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            }
        }
    }

    public override string ToString()
    {
        return
            $"Node: ID='{Id}', NewName='{NewName}', InGraph={Graph?.Name ?? ""}, ShapeID={Shape?.ID}, Attributes=[{string.Join("|", Attributes.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]";
    }
}