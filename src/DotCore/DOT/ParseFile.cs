using System;
using System.IO;
using GoldParser;

namespace DotCore.DOT;

public partial class DotParser
{
    public enum ProductionIndex
    {
        UnsignedInteger = 0, // <unsigned> ::= integer
        UnsignedFloat = 1, // <unsigned> ::= float
        Number = 2, // <number> ::= <unsigned>
        NumberPlus = 3, // <number> ::= '+' <unsigned>
        NumberMinus = 4, // <number> ::= '-' <unsigned>
        IdVariable = 5, // <id> ::= variable
        Id = 6, // <id> ::= <number>
        IdStringlit = 7, // <id> ::= stringlit
        GraphLbraceRbrace = 8, // <graph> ::= <strict> <graph type> <id> '{' <stmt list> '}'
        StrictStrict = 9, // <strict> ::= strict
        Strict = 10, // <strict> ::= 
        GraphtypeDigraph = 11, // <graph type> ::= digraph
        GraphtypeGraph = 12, // <graph type> ::= graph
        Stmtlist = 13, // <stmt list> ::= <stmt> <stmt list>
        StmtlistSemi = 14, // <stmt list> ::= <stmt> ';' <stmt list>
        Stmtlist2 = 15, // <stmt list> ::= 
        Stmt = 16, // <stmt> ::= <attr stmt>
        Stmt2 = 17, // <stmt> ::= <node stmt>
        Stmt3 = 18, // <stmt> ::= <edge stmt>
        Stmt4 = 19, // <stmt> ::= <subgraph stmt>
        Stmt5 = 20, // <stmt> ::= <attr Attribute>
        Attrstmt = 21, // <attr stmt> ::= <attr noun> <attr list>
        AttrnounGraph = 22, // <attr noun> ::= graph
        AttrnounNode = 23, // <attr noun> ::= node
        AttrnounEdge = 24, // <attr noun> ::= edge
        AttrlistLbracketRbracket = 25, // <attr list> ::= '[' <a list> ']'
        AttrlistLbracketRbracket2 = 26, // <attr list> ::= '[' ']'
        Alist = 27, // <a list> ::= <attr Attribute>
        AlistComma = 28, // <a list> ::= <attr Attribute> ',' <a list>
        Alist2 = 29, // <a list> ::= <attr Attribute> <a list>
        AttrattributeEq = 30, // <attr Attribute> ::= <id> '=' <id>
        Nodestmt = 31, // <node stmt> ::= <node id>
        Nodestmt2 = 32, // <node stmt> ::= <node id> <attr list>
        Nodeid = 33, // <node id> ::= <id>
        Nodeid2 = 34, // <node id> ::= <id> <port>
        Port = 35, // <port> ::= <port location>
        Port2 = 36, // <port> ::= <port angle>
        Port3 = 37, // <port> ::= <port location> <port angle>
        Port4 = 38, // <port> ::= <port angle> <port location>
        PortlocationColon = 39, // <port location> ::= ':' <id>
        PortlocationColonLparenCommaRparen = 40, // <port location> ::= ':' <id> '(' <id> ',' <id> ')'
        PortangleAt = 41, // <port angle> ::= '@' <id>
        Edgestmt = 42, // <edge stmt> ::= <node id> <edgeRHS>
        Edgestmt2 = 43, // <edge stmt> ::= <node id> <edgeRHS> <attr list>
        Edgestmt3 = 44, // <edge stmt> ::= <subgraph> <edgeRHS>
        Edgestmt4 = 45, // <edge stmt> ::= <subgraph> <edgeRHS> <attr list>
        EdgerhsEdgeop = 46, // <edgeRHS> ::= edgeop <node id>
        EdgerhsEdgeop2 = 47, // <edgeRHS> ::= edgeop <node id> <edgeRHS>
        SubgraphstmtSubgraphLbraceRbrace = 48, // <subgraph stmt> ::= subgraph <id> '{' <stmt list> '}'
        SubgraphstmtLbraceRbrace = 49, // <subgraph stmt> ::= '{' <stmt list> '}'
        SubgraphstmtSubgraphSemi = 50, // <subgraph stmt> ::= subgraph <id> ';'
        SubgraphSubgraph = 51, // <subgraph> ::= subgraph <id>
        SubgraphLbraceRbrace = 52, // <subgraph> ::= '{' <stmt list> '}'
        SubgraphSubgraphLbraceRbrace = 53 // <subgraph> ::= subgraph <id> '{' <stmt list> '}'
    }

    private bool ParseFile(string ifile)
    {
        using var reader = new StreamReader(ifile);

        var accepted = false;

        _parser.Open(reader);
        _parser.TrimReductions = false;

        var done = false;
        while (!done)
        {
            var response = _parser.Parse();

            switch (response)
            {
                case ParseMessage.LexicalError:
                {
                    // Cannot recognize token
                    throw new Exception(DescribeParserError(response.ToString(), ifile));
                }

                case ParseMessage.SyntaxError:
                {
                    // Expecting a different token
                    throw new Exception(DescribeParserError(response.ToString(), ifile));
                }

                case ParseMessage.Reduction:
                {
                    // Create a customized object to store the reduction
                    DoReduction((Reduction)_parser.CurrentReduction);
                    break;
                }

                case ParseMessage.Accept:
                {
                    // Accepted
                    done = true;
                    accepted = true;
                    break;
                }

                case ParseMessage.TokenRead:
                {
                    break;
                }

                case ParseMessage.InternalError:
                {
                    // INTERNAL ERROR! Something is wrong.
                    throw new Exception(DescribeParserError(response.ToString(), ifile));
                }

                case ParseMessage.NotLoadedError:
                {
                    // This error occurs if the CGT was not loaded.   
                    throw new Exception(DescribeParserError(response.ToString(), ifile));
                }

                case ParseMessage.GroupError:
                {
                    // COMMENT ERROR! Unexpected end of file
                    throw new Exception(DescribeParserError(response.ToString(), ifile));
                }
            }
        }

        return accepted;
    }
}