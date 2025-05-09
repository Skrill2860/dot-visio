using System.Collections.Generic;
using System.Globalization;
using Common;
using Domain;
using GoldParser;
using Microsoft.VisualBasic.CompilerServices;
using static System.Windows.Forms.Application;

namespace DotCore.DOT;

public partial class DotParser
{
    private void DoReduction(Reduction r)
    {
        DoEvents();

        if (r.Tokens.Count == 1) // LHS ::= RHS
        {
            if (((dynamic)r.Tokens[0]).Data is Reduction)
            {
                r.Tag = ((dynamic)r.Tokens[0]).Data.Tag;
            }
        }

        switch (r.Parent.TableIndex())
        {
            case (short)ProductionIndex.UnsignedInteger:
            {
                // <unsigned> ::= integer 
                r.Tag = ((dynamic)r.Tokens[0]).Data;
                break;
            }

            case (short)ProductionIndex.UnsignedFloat:
            {
                // <unsigned> ::= float 
                r.Tag = ((dynamic)r.Tokens[0]).Data;
                break;
            }

            case (short)ProductionIndex.Number:
            {
                // <number> ::= <unsigned> 
                r.Tag = ((dynamic)r.Tokens[0]).Data.Tag;
                break;
            }

            case (short)ProductionIndex.NumberPlus:
            {
                // <number> ::= '+' <unsigned> 
                r.Tag = ((dynamic)r.Tokens[1]).Data;
                break;
            }

            case (short)ProductionIndex.NumberMinus:
            {
                // <number> ::= '-' <unsigned>
                string strnum = (((dynamic)r.Tokens[1]).Data.Tag).ToString();
                r.Tag = (-double.Parse(strnum, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
                break;
            }

            case (short)ProductionIndex.IdVariable:
            {
                // <id> ::= variable 
                r.Tag = ((dynamic)r.Tokens[0]).Data;
                break;
            }

            case (short)ProductionIndex.Id:
            {
                // <id> ::= <number> 
                r.Tag = ((dynamic)r.Tokens[0]).Data.Tag;
                break;
            }

            case (short)ProductionIndex.IdStringlit:
            {
                // <id> ::= stringlit 
                r.Tag = StringExtensions.UnQuote(((dynamic)r.Tokens[0]).Data.ToString());
                break;
            }

            case (short)ProductionIndex.GraphLbraceRbrace:
            {
                // <graph> ::= <strict> <graph type> <id> '{' <stmt list> '}' 
                _graph.Strict = Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag) == "strict";
                _graph.IsDigraph = Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag) == "digraph";
                _graph.Name = SharedConstants.ROOTGRAPHNAME; // Really should be "CStr(r.Tokens(2).Data.Tag)" but we have to have a standard root name for some layer and subraph export reasons
                DoStatements(_graph, (List<object>)((dynamic)r.Tokens[4]).Data.Tag);
                r.Tag = _graph;
                break;
            }

            case (short)ProductionIndex.StrictStrict:
            {
                // <strict> ::= strict 
                r.Tag = "strict";
                break;
            }

            case (short)ProductionIndex.Strict:
            {
                // <strict> ::=  
                r.Tag = "";
                break;
            }

            case (short)ProductionIndex.GraphtypeDigraph:
            {
                // <graph type> ::= digraph
                r.Tag = "digraph";
                break;
            }

            case (short)ProductionIndex.GraphtypeGraph:
            {
                // <graph type> ::= graph 
                r.Tag = "graph";
                break;
            }

            case (short)ProductionIndex.Stmtlist:
            {
                // <stmt list> ::= <stmt> <stmt list> 
                var stmtlist = (List<object>)((dynamic)r.Tokens[1]).Data.Tag;
                stmtlist.Insert(0, ((dynamic)r.Tokens[0]).Data.Tag);
                r.Tag = stmtlist;
                break;
            }

            case (short)ProductionIndex.StmtlistSemi:
            {
                // <stmt list> ::= <stmt> ';' <stmt list> 
                var stmtlist = (List<object>)((dynamic)r.Tokens[2]).Data.Tag;
                stmtlist.Insert(0, ((dynamic)r.Tokens[0]).Data.Tag);
                r.Tag = stmtlist;
                break;
            }

            case (short)ProductionIndex.Stmtlist2:
            {
                // <stmt list> ::=  
                r.Tag = new List<object>();
                break;
            }

            case (short)ProductionIndex.Stmt:
            {
                break;
            }
            // <stmt> ::= <attr stmt> 
            // LHS ::= RHS

            case (short)ProductionIndex.Stmt2:
            {
                break;
            }
            // <stmt> ::= <node stmt> 
            // LHS ::= RHS

            case (short)ProductionIndex.Stmt3:
            {
                break;
            }
            // <stmt> ::= <edge stmt> 
            // LHS ::= RHS

            case (short)ProductionIndex.Stmt4:
            {
                break;
            }
            // <stmt> ::= <subgraph stmt> 
            // LHS ::= RHS

            case (short)ProductionIndex.Stmt5:
            {
                break;
            }
            // <stmt> ::= <attr Attribute> 
            // LHS ::= RHS

            case (short)ProductionIndex.Attrstmt:
            {
                // <attr stmt> ::= <attr noun> <attr list> 
                var node = (Node)((dynamic)r.Tokens[0]).Data.Tag;
                AddRange(ref node.Attributes, (List<Attribute>)((dynamic)r.Tokens[1]).Data.Tag);
                r.Tag = node;
                break;
            }

            case (short)ProductionIndex.AttrnounGraph:
            {
                // <attr noun> ::= graph 
                r.Tag = new Node(" graph");
                break;
            }

            case (short)ProductionIndex.AttrnounNode:
            {
                // <attr noun> ::= node 
                r.Tag = new Node(" node");
                break;
            }

            case (short)ProductionIndex.AttrnounEdge:
            {
                // <attr noun> ::= edge 
                r.Tag = new Node(" edge");
                break;
            }

            case (short)ProductionIndex.AttrlistLbracketRbracket:
            {
                // <attr list> ::= '[' <a list> ']' 
                r.Tag = ((dynamic)r.Tokens[1]).Data.Tag;
                break;
            }

            case (short)ProductionIndex.AttrlistLbracketRbracket2:
            {
                // <attr list> ::= '[' ']' 
                r.Tag = null;
                break;
            }

            case (short)ProductionIndex.Alist:
            {
                break;
            }
            // <a list> ::= <attr Attribute> 
            // LHS ::= RHS

            case (short)ProductionIndex.AlistComma:
            {
                // <a list> ::= <attr Attribute> ',' <a list> 
                var attrlist = (List<Attribute>)((dynamic)r.Tokens[0]).Data.Tag;
                var adding = (List<Attribute>)((dynamic)r.Tokens[2]).Data.Tag;
                attrlist.AddRange(adding);
                r.Tag = attrlist;
                break;
            }

            case (short)ProductionIndex.Alist2:
            {
                // <a list> ::= <attr Attribute> <a list> 
                var attrlist = (List<Attribute>)((dynamic)r.Tokens[0]).Data.Tag;
                List<Attribute> adding = (List<Attribute>)((dynamic)r.Tokens[1]).Data.Tag;
                attrlist.AddRange(adding);
                r.Tag = attrlist;
                break;
            }

            case (short)ProductionIndex.AttrattributeEq:
            {
                // <attr Attribute> ::= <id> '=' <id>
                var attrlist = new List<Attribute>();
                var attr = new Attribute(Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag),
                    Conversions.ToString(((dynamic)r.Tokens[2]).Data.Tag));
                attrlist.Add(attr);
                r.Tag = attrlist;
                break;
            }

            case (short)ProductionIndex.Nodestmt:
            {
                // <node stmt> ::= <node id> 
                var node = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag));
                r.Tag = node;
                break;
            }

            case (short)ProductionIndex.Nodestmt2:
            {
                // <node stmt> ::= <node id> <attr list> 
                var node = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag));
                foreach (var attr in (List<Attribute>)((dynamic)r.Tokens[1]).Data.Tag)
                {
                    node.SetAttribute(attr.Lhs, attr.Rhs);
                }

                r.Tag = node;
                break;
            }

            case (short)ProductionIndex.Nodeid:
            {
                // <node id> ::= <id>
                break;
            }

            case (short)ProductionIndex.Nodeid2:
            case (short)ProductionIndex.Port:
            case (short)ProductionIndex.Port2:
            case (short)ProductionIndex.Port3:
            case (short)ProductionIndex.Port4:
            case (short)ProductionIndex.PortlocationColon:
            case (short)ProductionIndex.PortlocationColonLparenCommaRparen:
            case (short)ProductionIndex.PortangleAt:
            {
                // ports are not supported

                break;
            }

            case (short)ProductionIndex.Edgestmt:
            {
                // <edge stmt> ::= <node id> <edgeRHS> 
                List<Edge> edges = (List<Edge>)((dynamic)r.Tokens[1]).Data.Tag;
                edges[0].FromNode = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag));
                r.Tag = edges;
                break;
            }

            case (short)ProductionIndex.Edgestmt2:
            {
                // <edge stmt> ::= <node id> <edgeRHS> <attr list> 
                List<Edge> edges = (List<Edge>)((dynamic)r.Tokens[1]).Data.Tag;
                edges[0].FromNode = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[0]).Data.Tag));
                foreach (var edge in edges)
                {
                    foreach (var attr in (List<Attribute>)((dynamic)r.Tokens[2]).Data.Tag)
                    {
                        edge.SetAttribute(attr.Lhs, attr.Rhs);
                    }
                }

                r.Tag = edges;
                break;
            }

            case (short)ProductionIndex.Edgestmt3:
            {
                // <edge stmt> ::= <subgraph> <edgeRHS> 
                var subgraph = (Graph)((dynamic)r.Tokens[0]).Data.Tag;
                List<Edge> edges = (List<Edge>)((dynamic)r.Tokens[1]).Data.Tag;
                var first = true;
                foreach (var node in subgraph.Nodes)
                {
                    foreach (var edge in edges)
                    {
                        if (first)
                        {
                            var newedgefrom = new Edge();
                            newedgefrom.FromNode = node;
                            newedgefrom.ToNode = edge.FromNode;
                            first = false;
                        }

                        var newedge = new Edge();
                        newedge.FromNode = node;
                        newedge.ToNode = edge.ToNode;
                    }
                }

                var edgestmt = new List<object>();
                edgestmt.Add(subgraph);
                edgestmt.Add(edges);
                r.Tag = edgestmt;
                break;
            }

            case (short)ProductionIndex.Edgestmt4:
            {
                // <edge stmt> ::= <subgraph> <edgeRHS> <attr list> 
                var subgraph = (Graph)((dynamic)r.Tokens[0]).Data.Tag;
                List<Edge> edges = (List<Edge>)((dynamic)r.Tokens[1]).Data.Tag;
                var first = true;
                foreach (var node in subgraph.Nodes)
                {
                    foreach (var edge in edges)
                    {
                        if (first)
                        {
                            var newedgefrom = new Edge();
                            newedgefrom.FromNode = node;
                            newedgefrom.ToNode = edge.FromNode;
                            first = false;
                        }

                        var newedge = new Edge();
                        newedge.FromNode = node;
                        newedge.ToNode = edge.ToNode;
                    }
                }

                foreach (var edge in edges)
                {
                    foreach (var attr in (List<Attribute>)((dynamic)r.Tokens[2]).Data.Tag)
                    {
                        edge.SetAttribute(attr.Lhs, attr.Rhs);
                    }
                }

                var edgestmt = new List<object>();
                edgestmt.Add(subgraph);
                edgestmt.Add(edges);
                r.Tag = edgestmt;
                break;
            }

            case (short)ProductionIndex.EdgerhsEdgeop:
            {
                // <edgeRHS> ::= edgeop <node id> 
                var edges = new List<Edge>();
                var edge = new Edge();
                edge.ToNode = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                edges.Add(edge);
                r.Tag = edges;
                break;
            }

            case (short)ProductionIndex.EdgerhsEdgeop2:
            {
                // <edgeRHS> ::= edgeop <node id> <edgeRHS> 
                var edge = new Edge();
                var node = _graph.FindNode(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                edge.ToNode = node;
                List<Edge> edges = (List<Edge>)((dynamic)r.Tokens[2]).Data.Tag;
                edges[0].FromNode = node;
                edges.Insert(0, edge);
                r.Tag = edges;
                break;
            }

            case (short)ProductionIndex.SubgraphstmtSubgraphLbraceRbrace:
            {
                // <subgraph stmt> ::= subgraph <id> '{' <stmt list> '}' 
                var Subgraph = new Graph(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                DoStatements(Subgraph, (List<object>)((dynamic)r.Tokens[3]).Data.Tag);
                r.Tag = Subgraph;
                break;
            }

            case (short)ProductionIndex.SubgraphstmtLbraceRbrace:
            {
                // <subgraph stmt> ::= '{' <stmt list> '}' 
                var Subgraph = new Graph(UniqueNameGenerator.GenerateUniqueName("cluster_"));
                DoStatements(Subgraph, (List<object>)((dynamic)r.Tokens[1]).Data.Tag);
                r.Tag = Subgraph;
                break;
            }

            case (short)ProductionIndex.SubgraphstmtSubgraphSemi:
            {
                // <subgraph stmt> ::= subgraph <id> ';' 
                var Subgraph = new Graph(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                r.Tag = Subgraph;
                break;
            }

            case (short)ProductionIndex.SubgraphSubgraph:
            {
                // <subgraph> ::= subgraph <id> 
                var Subgraph = new Graph(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                r.Tag = Subgraph;
                break;
            }

            case (short)ProductionIndex.SubgraphLbraceRbrace:
            {
                // <subgraph> ::= '{' <stmt list> '}' 
                var Subgraph = new Graph(UniqueNameGenerator.GenerateUniqueName("cluster_"));
                DoStatements(Subgraph, (List<object>)((dynamic)r.Tokens[1]).Data.Tag);
                r.Tag = Subgraph;
                break;
            }

            case (short)ProductionIndex.SubgraphSubgraphLbraceRbrace:
            {
                // <subgraph> ::= subgraph <id> '{' <stmt list> '}' 
                var Subgraph = new Graph(Conversions.ToString(((dynamic)r.Tokens[1]).Data.Tag));
                DoStatements(Subgraph, (List<object>)((dynamic)r.Tokens[3]).Data.Tag);
                r.Tag = Subgraph;
                break;
            }

            default:
            {
                _parser.Close();

                throw new DotVisioException("ReadDOT reduction error" + " at line " + _parser.CurrentPosition().Line + " column " +
                                            _parser.CurrentPosition().Column + " token '" + _parser.CurrentToken() +
                                            "' Reduction " + string.Join("", r.Tokens.ToArray()) + " is unknown");
            }
        }
    }
}