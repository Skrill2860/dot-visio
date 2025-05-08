using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Common;
using Domain;
using GoldParser;

namespace DotCore.DOT;

public partial class DotParser
{
    private readonly Parser _parser;
    private Graph _graph;

    public DotParser()
    {
        Assembly asm = Assembly.GetExecutingAssembly();

        string[] resNames = asm.GetManifestResourceNames();
        var grammarFileRes = resNames.First(res => res.EndsWith(SharedConstants.GRAMMARFILE));

        Stream grammarFileResStream = asm.GetManifestResourceStream(grammarFileRes);

        _parser = new Parser(grammarFileResStream);
    }

    private void AddRange(ref Dictionary<string, string> @base, List<Attribute> adding)
    {
        var found = "";
        foreach (var attr in adding)
        {
            @base[attr.Lhs] = attr.Rhs;
        }
    }

    private void AddRange(ref Dictionary<string, string> @base, Dictionary<string, string> adding)
    {
        var found = "";
        foreach (KeyValuePair<string, string> kvp in adding)
        {
            @base[kvp.Key] = kvp.Value;
        }
    }

    private string DescribeParserError(string msg, string filename)
    {
        var ans = "";
        try
        {
            var token = "[no current token]";
            if (_parser.CurrentToken() is not null)
            {
                token = _parser.CurrentToken().Data.ToString();
            }

            var symbols = "[no valid symbols]";
            if (_parser.ExpectedSymbols() is not null)
            {
                symbols = _parser.ExpectedSymbols().Text();
            }

            ans = msg + " in " + filename + " at line " + _parser.CurrentPosition().Line + " column " + _parser.CurrentPosition().Column +
                  ". Current token: '" + token + "'. Valid tokens :'" + symbols + "'";
        }
        catch (Exception ex)
        {
            ans = "Unable to describe parse error: " + ex;
        }

        return ans;
    }

    public void DoStatement(Graph graph, object stmt)
    {
        if (stmt is Node)
        {
            var node = (Node)stmt;
            if (CultureInfo.CurrentCulture.CompareInfo.Compare(node.Id, " graph",
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            {
                AddRange(ref graph.Attributes, node.Attributes);
            }
            else if (CultureInfo.CurrentCulture.CompareInfo.Compare(node.Id, " node",
                         CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            {
                AddRange(ref graph.DefaultNodeAttributes, node.Attributes);
            }
            else if (CultureInfo.CurrentCulture.CompareInfo.Compare(node.Id, " edge",
                         CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            {
                AddRange(ref graph.DefaultEdgeAttributes, node.Attributes);
            }
            else if (node.Graph is null)
            {
                graph.AddNode(node);
            }
        }
        else if (stmt is Edge)
        {
            graph.AddEdge((Edge)stmt);
        }
        else if (stmt is List<Edge>)
        {
            foreach (var edge in (List<Edge>)stmt)
            {
                graph.AddEdge(edge);
            }
        }
        else if (stmt is Graph)
        {
            graph.AddSubGraph((Graph)stmt);
        }
        else if (stmt is List<Attribute>)
        {
            AddRange(ref graph.Attributes, (List<Attribute>)stmt);
        }
    }

    private void DoStatements(Graph graph, List<object> stmts)
    {
        foreach (var stmt in stmts)
        {
            if (stmt is List<object>)
            {
                foreach (var obj in (IEnumerable<object>)stmt)
                {
                    DoStatement(graph, stmt);
                }
            }
            else
            {
                DoStatement(graph, stmt);
            }
        }
    }

    public Graph LoadDot(string ifile)
    {
        _graph = new Graph(SharedConstants.ROOTGRAPHNAME);

        if (ParseFile(ifile))
        {
            return _graph;
        }

        throw new Exception("Unable to parse " + ifile);
    }

    private class Attribute
    {
        public readonly string Lhs;
        public readonly string Rhs;

        public Attribute(string var, string val)
        {
            Lhs = var;
            Rhs = val;
        }
    }
}