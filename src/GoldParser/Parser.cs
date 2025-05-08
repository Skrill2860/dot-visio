using System;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GoldParser;

public class ParserException : Exception
{
    public string Method;

    public ParserException(string message) : base(message)
    {
        Method = "";
    }

    public ParserException(string message, Exception inner, string method) : base(message, inner)
    {
        Method = method;
    }
}

// ===== Parsing messages 
public enum ParseMessage
{
    TokenRead = 0, // A new token is read
    Reduction = 1, // A production is reduced
    Accept = 2, // Grammar complete
    NotLoadedError = 3, // The tables are not loaded
    LexicalError = 4, // Token not recognized
    SyntaxError = 5, // Token is not expected
    GroupError = 6, // Reached the end of the file inside a block
    InternalError = 7 // Something is wrong, very wrong
}

public class GrammarProperties
{
    private const int PropertyCount = 8;

    private readonly string[] m_Property = new string[9];

    public GrammarProperties()
    {
        for (var n = 0; n <= PropertyCount - 1; n++)
        {
            m_Property[n] = "";
        }
    }

    public string Name => m_Property[(int)PropertyIndex.Name];

    public string Version => m_Property[(int)PropertyIndex.Version];

    public string Author => m_Property[(int)PropertyIndex.Author];

    public string About => m_Property[(int)PropertyIndex.About];

    public string CharacterSet => m_Property[(int)PropertyIndex.CharacterSet];

    public string CharacterMapping => m_Property[(int)PropertyIndex.CharacterMapping];

    public string GeneratedBy => m_Property[(int)PropertyIndex.GeneratedBy];

    public string GeneratedDate => m_Property[(int)PropertyIndex.GeneratedDate];

    public void SetValue(int Index, string Value)
    {
        if ((Index >= 0) & (Index < PropertyCount))
        {
            m_Property[Index] = Value;
        }
    }

    private enum PropertyIndex
    {
        Name = 0,
        Version = 1,
        Author = 2,
        About = 3,
        CharacterSet = 4,
        CharacterMapping = 5,
        GeneratedBy = 6,
        GeneratedDate = 7
    }
}

public class Parser
{
    // ===================================================================
    // Class Name:
    // Parser
    // 
    // Purpose:
    // This is the main class in the GOLD Parser Engine and is used to perform
    // all duties required to the parsing of a source text string. This class
    // contains the LALR(1) State Machine code, the DFA State Machine code,
    // character table (used by the DFA algorithm) and all other structures and
    // methods needed to interact with the developer.
    // 
    // Author(s):
    // Devin Cook
    // 
    // Public Dependencies:
    // Token, TokenList, Production, ProductionList, Symbol, SymbolList, Reduction, Position
    // 
    // Private Dependencies:
    // CGTReader, TokenStack, TokenStackQueue, FAStateList, CharacterRange, CharacterSet,
    // CharacterSetList, LRActionTableList
    // 
    // Revision History:    
    // 2011-10-06
    // * Added 5.0 logic.
    // ===================================================================

    private const string kVersion = "5.0";
    private readonly Position m_CurrentPosition = new(); // Last read terminal

    // ===== Used for Reductions & Errors
    private readonly SymbolList m_ExpectedSymbols = new(); // This ENTIRE list will available to the user

    // ===== Lexical Groups
    private readonly TokenStack m_GroupStack = new();
    private readonly TokenQueueStack m_InputTokens = new(); // Tokens to be analyzed - Hybred object!
    private readonly TokenStack m_Stack = new();

    // === Line and column information. 
    private readonly Position m_SysPosition = new(); // Internal - so user cannot mess with values
    private CharacterSetList m_CharSetTable = new();
    private int m_CurrentLALR;

    // ===== DFA
    private FaStateList m_DFA = new();

    // ===== Grammar Attributes
    private GrammarProperties m_Grammar = new();
    private GroupList m_GroupTable = new();
    private bool m_HaveReduction;
    private string m_LookaheadBuffer;

    // ===== LALR
    private LrStateList m_LRStates = new();

    // ===== Productions
    private ProductionList m_ProductionTable = new();

    private TextReader m_Source;

    // ===== Symbols recognized by the system
    private SymbolList m_SymbolTable = new();

    // ===== Private control variables
    private bool m_TablesLoaded;

    public Parser(string grammarfile)
    {
        m_TablesLoaded = false;
        LoadTables(grammarfile);
        Restart();

        // ======= Default Properties
        TrimReductions = false;
    }

    public Parser(Stream grammarfileResourceStream)
    {
        m_TablesLoaded = false;
        LoadTables(new BinaryReader(grammarfileResourceStream));
        Restart();

        // ======= Default Properties
        TrimReductions = false;
    }

    [Description("When the Parse() method returns a Reduce, this method will contain the current Reduction.")]
    public object CurrentReduction
    {
        get
        {
            object CurrentReductionRet = default;
            if (m_HaveReduction)
            {
                CurrentReductionRet = m_Stack.Top().Data;
            }
            else
            {
                CurrentReductionRet = null;
            }

            return CurrentReductionRet;
        }
        set
        {
            if (m_HaveReduction)
            {
                m_Stack.Top().Data = value;
            }
        }
    }

    [Description("Determines if reductions will be trimmed in cases where a production contains a single element.")]
    public bool TrimReductions { get; set; }

    public void Close()
    {
        if (m_Source is not null)
        {
            m_Source.Close();
        }
    }

    [Description("Opens a string for parsing.")]
    public bool Open(ref string Text)
    {
        return Open(new StringReader(Text));
    }

    [Description("Opens a text stream for parsing.")]
    public bool Open(TextReader Reader)
    {
        var Start = new Token();

        Restart();
        m_Source = Reader;

        // === Create stack top item. Only needs state
        Start.State = m_LRStates.InitialState;
        m_Stack.Push(ref Start);

        return true;
    }

    [Description("Returns information about the current grammar.")]
    public GrammarProperties Grammar()
    {
        return m_Grammar;
    }

    [Description("Current line and column being read from the source.")]
    public Position CurrentPosition()
    {
        return m_CurrentPosition;
    }

    [Description("If the Parse() function returns TokenRead, this method will return that last read token.")]
    public Token CurrentToken()
    {
        return m_InputTokens.Top();
    }

    [Description("Removes the next token from the input queue.")]
    public Token DiscardCurrentToken()
    {
        return m_InputTokens.Dequeue();
    }

    [Description("Added a token onto the end of the input queue.")]
    public void EnqueueInput(ref Token TheToken)
    {
        m_InputTokens.Enqueue(ref TheToken);
    }

    [Description("Pushes the token onto the top of the input queue. This token will be analyzed next.")]
    public void PushInput(ref Token TheToken)
    {
        m_InputTokens.Push(TheToken);
    }

    private string LookaheadBuffer(int Count)
    {
        // Return Count characters from the lookahead buffer. DO NOT CONSUME
        // This is used to create the text stored in a token. It is disgarded
        // separately. Because of the design of the DFA algorithm, count should
        // never exceed the buffer length. The If-Statement below is fault-tolerate
        // programming, but not necessary.

        if (Count > m_LookaheadBuffer.Length)
        {
            Count = Conversions.ToInteger(m_LookaheadBuffer);
        }

        return m_LookaheadBuffer.Substring(0, Count);
    }

    private string Lookahead(int CharIndex)
    {
        // Return single char at the index. This function will also increase 
        // buffer if the specified character is not present. It is used 
        // by the DFA algorithm.

        // Check if we must read characters from the Stream
        if (CharIndex > m_LookaheadBuffer.Length)
        {
            var ReadCount = CharIndex - m_LookaheadBuffer.Length;
            var loopTo = ReadCount;
            for (var n = 1; n <= loopTo; n++)
            {
                m_LookaheadBuffer += Conversions.ToString(Strings.ChrW(m_Source.Read()));
            }
        }

        // If the buffer is still smaller than the index, we have reached
        // the end of the text. In this case, return a null string - the DFA
        // code will understand.
        if (CharIndex <= m_LookaheadBuffer.Length)
        {
            return Conversions.ToString(m_LookaheadBuffer[CharIndex - 1]);
        }

        return "";
    }

    [Description("Library name and version.")]
    public string About()
    {
        return "GOLD Parser Engine; Version " + kVersion;
    }

    public void Clear()
    {
        m_SymbolTable.Clear();
        m_ProductionTable.Clear();
        m_CharSetTable.Clear();
        m_DFA.Clear();
        m_LRStates.Clear();

        m_Stack.Clear();
        m_InputTokens.Clear();

        m_Grammar = new GrammarProperties();

        m_GroupStack.Clear();
        m_GroupTable.Clear();

        Restart();
    }

    [Description("Loads parse tables from the specified filename. Only EGT (version 5.0) is supported.")]
    public bool LoadTables(string path)
    {
        return LoadTables(new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)));
    }

    [Description("Loads parse tables from the specified BinaryReader. Only EGT (version 5.0) is supported.")]
    public bool LoadTables(BinaryReader reader)
    {
        bool success;

        try
        {
            var egt = new EgtReader(reader);

            Restart();
            success = true;
            while (!(egt.EndOfFile() | (success == false)))
            {
                egt.GetNextRecord();

                var recType = (EgtRecord)egt.RetrieveByte();

                switch (recType)
                {
                    case EgtRecord.Property:
                    {
                        // Index, Name, Value

                        var index = egt.RetrieveInt16();
                        var name = egt.RetrieveString(); // Just discard
                        m_Grammar.SetValue(index, egt.RetrieveString());
                        break;
                    }

                    case EgtRecord.TableCounts:
                    {
                        // Symbol, CharacterSet, Rule, DFA, LALR
                        m_SymbolTable = new SymbolList(egt.RetrieveInt16());
                        m_CharSetTable = new CharacterSetList(egt.RetrieveInt16());
                        m_ProductionTable = new ProductionList(egt.RetrieveInt16());
                        m_DFA = new FaStateList(egt.RetrieveInt16());
                        m_LRStates = new LrStateList(egt.RetrieveInt16());
                        m_GroupTable = new GroupList(egt.RetrieveInt16());
                        break;
                    }

                    case EgtRecord.InitialStates:
                    {
                        // DFA, LALR
                        m_DFA.InitialState = (short)egt.RetrieveInt16();
                        m_LRStates.InitialState = (short)egt.RetrieveInt16();
                        break;
                    }

                    case EgtRecord.Symbol:
                    {
                        // #, Name, Kind

                        var index = egt.RetrieveInt16();
                        var name = egt.RetrieveString();
                        var type = (SymbolType)egt.RetrieveInt16();

                        m_SymbolTable[index] = new Symbol(name, type, (short)index);
                        break;
                    }

                    case EgtRecord.Group:
                    {
                        // #, Name, Container#, Start#, End#, Tokenized, Open Ended, Reserved, Count, (Nested Group #...) 
                        var g = new Group();

                        var index = egt.RetrieveInt16(); // # 

                        g.Name = egt.RetrieveString();
                        g.Container = SymbolTable()[egt.RetrieveInt16()];
                        g.Start = SymbolTable()[egt.RetrieveInt16()];
                        g.End = SymbolTable()[egt.RetrieveInt16()];

                        g.Advance = (Group.AdvanceMode)egt.RetrieveInt16();
                        g.Ending = (Group.EndingMode)egt.RetrieveInt16();
                        egt.RetrieveEntry(); // Reserved

                        var count = egt.RetrieveInt16();
                        for (var n = 1; n <= count; n++)
                        {
                            g.Nesting.Add(egt.RetrieveInt16());
                        }

                        // === Link back
                        g.Container.Group = g;
                        g.Start.Group = g;
                        g.End.Group = g;

                        m_GroupTable[index] = g;
                        break;
                    }

                    case EgtRecord.CharRanges:
                    {
                        // #, Total Sets, RESERVED, (Start#, End#  ...)

                        var index = egt.RetrieveInt16();
                        egt.RetrieveInt16(); // Codepage
                        var total = egt.RetrieveInt16();
                        egt.RetrieveEntry(); // Reserved

                        m_CharSetTable[index] = new CharacterSet();
                        while (!egt.RecordComplete())
                        {
                            var argItem = new CharacterRange((ushort)egt.RetrieveInt16(), (ushort)egt.RetrieveInt16());
                            m_CharSetTable[index].Add(ref argItem);
                        }

                        break;
                    }

                    case EgtRecord.Production:
                    {
                        // #, ID#, Reserved, (Symbol#,  ...)

                        var index = egt.RetrieveInt16();
                        var headIndex = egt.RetrieveInt16();
                        egt.RetrieveEntry(); // Reserved

                        m_ProductionTable[index] = new Production(m_SymbolTable[headIndex], (short)index);

                        while (!egt.RecordComplete())
                        {
                            var symIndex = egt.RetrieveInt16();
                            m_ProductionTable[index].Handle().Add(m_SymbolTable[symIndex]);
                        }

                        break;
                    }

                    case EgtRecord.DfaState:
                    {
                        // #, Accept?, Accept#, Reserved (CharSet#, Target#, Reserved)...

                        var index = egt.RetrieveInt16();
                        var accept = egt.RetrieveBoolean();
                        var acceptIndex = egt.RetrieveInt16();
                        egt.RetrieveEntry(); // Reserved

                        if (accept)
                        {
                            m_DFA[index] = new FaState(m_SymbolTable[acceptIndex]);
                        }
                        else
                        {
                            m_DFA[index] = new FaState();
                        }

                        // (Edge chars, Target#, Reserved)...
                        while (!egt.RecordComplete())
                        {
                            var setIndex = egt.RetrieveInt16(); // Char table index
                            var target = egt.RetrieveInt16(); // Target
                            egt.RetrieveEntry(); // Reserved

                            m_DFA[index].Edges.Add(new FaEdge(m_CharSetTable[setIndex], target));
                        }

                        break;
                    }

                    case EgtRecord.LrState:
                    {
                        // #, Reserved (Symbol#, Action, Target#, Reserved)...

                        var index = egt.RetrieveInt16();
                        egt.RetrieveEntry(); // Reserved

                        m_LRStates[index] = new LrState();

                        // (Symbol#, Action, Target#, Reserved)...
                        while (!egt.RecordComplete())
                        {
                            var symIndex = egt.RetrieveInt16();
                            var action = egt.RetrieveInt16();
                            var target = egt.RetrieveInt16();
                            egt.RetrieveEntry(); // Reserved

                            m_LRStates[index].Add(new LrAction(m_SymbolTable[symIndex], (LrActionType)action, (short)target));
                        } // RecordIDComment

                        break;
                    }

                    default:
                    {
                        throw new ParserException("File Error. A record of type '" + Strings.ChrW((int)recType) +
                                                  "' was read. This is not a valid code.");
                    }
                }
            }

            egt.Close();
        }

        catch (Exception ex)
        {
            throw new ParserException(ex.Message, ex, "LoadTables");
        }

        m_TablesLoaded = success;

        return success;
    }

    [Description("Returns a list of Symbols recognized by the grammar.")]
    public SymbolList SymbolTable()
    {
        return m_SymbolTable;
    }

    [Description("Returns a list of Productions recognized by the grammar.")]
    public ProductionList ProductionTable()
    {
        return m_ProductionTable;
    }

    [Description(
        "If the Parse() method returns a SyntaxError, this method will contain a list of the symbols the grammar expected to see.")]
    public SymbolList ExpectedSymbols()
    {
        return m_ExpectedSymbols;
    }

    private ParseResult ParseLALR(ref Token NextToken)
    {
        // This function analyzes a token and either:
        // 1. Makes a SINGLE reduction and pushes a complete Reduction object on the m_Stack
        // 2. Accepts the token and shifts
        // 3. Errors and places the expected symbol indexes in the Tokens list
        // The Token is assumed to be valid and WILL be checked
        // If an action is performed that requires controlt to be returned to the user, the function returns true.
        // The Message parameter is then set to the type of action.

        short n;
        Token Head;
        var Result = default(ParseResult);

        var ParseAction = m_LRStates[m_CurrentLALR][NextToken.Parent];

        if (ParseAction is not null) // Work - shift or reduce
        {
            m_HaveReduction = false; // Will be set true if a reduction is made
            // 'Debug.WriteLine("Action: " & ParseAction.Text)

            switch (ParseAction.Type)
            {
                case LrActionType.Accept:
                {
                    m_HaveReduction = true;
                    Result = ParseResult.Accept;
                    break;
                }

                case LrActionType.Shift:
                {
                    m_CurrentLALR = ParseAction.Value;
                    NextToken.State = (short)m_CurrentLALR;
                    m_Stack.Push(ref NextToken);
                    Result = ParseResult.Shift;
                    break;
                }

                case LrActionType.Reduce:
                {
                    // Produce a reduction - remove as many tokens as members in the rule & push a nonterminal token
                    var Prod = m_ProductionTable[ParseAction.Value];

                    // ======== Create Reduction
                    if (TrimReductions & Prod.ContainsOneNonTerminal())
                    {
                        // The current rule only consists of a single nonterminal and can be trimmed from the
                        // parse tree. Usually we create a new Reduction, assign it to the Data property
                        // of Head and push it on the m_Stack. However, in this case, the Data property of the
                        // Head will be assigned the Data property of the reduced token (i.e. the only one
                        // on the m_Stack).
                        // In this case, to save code, the value popped of the m_Stack is changed into the head.

                        Head = m_Stack.Pop();
                        Head.Parent = Prod.Head();

                        Result = ParseResult.ReduceEliminated;
                    }
                    else // Build a Reduction
                    {
                        m_HaveReduction = true;
                        var NewReduction = new Reduction(Prod.Handle().Count());

                        NewReduction.Parent = Prod;
                        for (n = (short)(Prod.Handle().Count() - 1); n >= 0; n += -1)
                        {
                            NewReduction[n] = m_Stack.Pop();
                        }

                        Head = new Token(Prod.Head(), NewReduction);
                        Result = ParseResult.ReduceNormal;
                    }

                    // ========== Goto
                    var Index = m_Stack.Top().State;

                    // ========= If n is -1 here, then we have an Internal Table Error!!!!
                    n = m_LRStates[Index].IndexOf(Prod.Head());
                    if (n != -1)
                    {
                        m_CurrentLALR = m_LRStates[Index][n].Value;

                        Head.State = (short)m_CurrentLALR;
                        m_Stack.Push(ref Head);
                    }
                    else
                    {
                        Result = ParseResult.InternalError;
                    }

                    break;
                }
            }
        }

        else
        {
            // === Syntax Error! Fill Expected Tokens
            m_ExpectedSymbols.Clear();
            foreach (LrAction Action in m_LRStates[m_CurrentLALR]) // .Count - 1
            {
                switch (Action.Symbol.Type)
                {
                    case SymbolType.Content:
                    case SymbolType.End:
                    case SymbolType.GroupStart:
                    case SymbolType.GroupEnd:
                    {
                        m_ExpectedSymbols.Add(Action.Symbol);
                        break;
                    }
                }
            }

            Result = ParseResult.SyntaxError;
        }

        return Result; // Very important
    }

    [Description("Restarts the parser. Loaded tables are retained.")]
    public void Restart()
    {
        m_CurrentLALR = m_LRStates.InitialState;

        // === Lexer
        m_SysPosition.Column = 0;
        m_SysPosition.Line = 0;
        m_CurrentPosition.Line = 0;
        m_CurrentPosition.Column = 0;

        m_HaveReduction = false;

        m_ExpectedSymbols.Clear();
        m_InputTokens.Clear();
        m_Stack.Clear();
        m_LookaheadBuffer = "";

        // ==== V4
        m_GroupStack.Clear();
    }

    [Description("Returns true if parse tables were loaded.")]
    public bool TablesLoaded()
    {
        return m_TablesLoaded;
    }

    private Token LookaheadDFA()
    {
        // This function implements the DFA for th parser's lexer.
        // It generates a token which is used by the LALR state
        // machine.

        int Target = default;
        bool Found;
        var Result = new Token();

        // ===================================================
        // Match DFA token
        // ===================================================
        var Done = false;
        int CurrentDFA = m_DFA.InitialState;
        var CurrentPosition = 1; // Next byte in the input Stream
        var LastAcceptState = -1; // We have not yet accepted a character string
        var LastAcceptPosition = -1;

        var Ch = Lookahead(1);
        if (!(string.IsNullOrEmpty(Ch) | (Strings.AscW(Ch) == 65535))) // NO MORE DATA
        {
            while (!Done)
            {
                // This code searches all the branches of the current DFA state
                // for the next character in the input Stream. If found the
                // target state is returned.

                Ch = Lookahead(CurrentPosition);
                if (string.IsNullOrEmpty(Ch)) // End reached, do not match
                {
                    Found = false;
                }
                else
                {
                    var n = 0;
                    Found = false;
                    while ((n < m_DFA[CurrentDFA].Edges.Count) & !Found)
                    {
                        var Edge = m_DFA[CurrentDFA].Edges[n];

                        // ==== Look for character in the Character Set Table
                        if (Edge.Characters.Contains(Strings.AscW(Ch)))
                        {
                            Found = true;
                            Target = Edge.Target; // .TableIndex
                        }

                        n += 1;
                    }
                }

                // This block-if statement checks whether an edge was found from the current state. If so, the state and current
                // position advance. Otherwise it is time to exit the main loop and report the token found (if there was one). 
                // If the LastAcceptState is -1, then we never found a match and the Error Token is created. Otherwise, a new 
                // token is created using the Symbol in the Accept State and all the characters that comprise it.

                if (Found)
                {
                    // This code checks whether the target state accepts a token.
                    // If so, it sets the appropiate variables so when the
                    // algorithm in done, it can return the proper token and
                    // number of characters.

                    if (m_DFA[Target].Accept is not null) // NOT is very important!
                    {
                        LastAcceptState = Target;
                        LastAcceptPosition = CurrentPosition;
                    }

                    CurrentDFA = Target;
                    CurrentPosition += 1;
                }

                else // No edge found
                {
                    Done = true;
                    if (LastAcceptState == -1) // Lexer cannot recognize symbol
                    {
                        Result.Parent = m_SymbolTable.GetFirstOfType(SymbolType.Error);
                        Result.Data = LookaheadBuffer(1);
                    }
                    else // Create Token, read characters
                    {
                        Result.Parent = m_DFA[LastAcceptState].Accept;
                        Result.Data = LookaheadBuffer(LastAcceptPosition);
                    } // Data contains the total number of accept characters
                }
                // DoEvents
            }
        }

        else
        {
            // End of file reached, create End Token
            Result.Data = "";
            Result.Parent = m_SymbolTable.GetFirstOfType(SymbolType.End);
        }

        // ===================================================
        // Set the new token's position information
        // ===================================================
        // Notice, this is a copy, not a linking of an instance. We don't want the user 
        // to be able to alter the main value indirectly.
        Result.Position().Copy(m_SysPosition);

        return Result;
    }

    private void ConsumeBuffer(int CharCount)
    {
        // Consume/Remove the characters from the front of the buffer. 

        if (CharCount <= m_LookaheadBuffer.Length)
        {
            // Count Carriage Returns and increment the public column and line
            // numbers. This is done for the Developer and is not necessary for the
            // DFA algorithm.
            var loopTo = CharCount - 1;
            for (var n = 0; n <= loopTo; n++)
            {
                switch (m_LookaheadBuffer[n])
                {
                    case '\n':
                    {
                        m_SysPosition.Line += 1;
                        // Ignore, LF is used to inc line to be UNIX friendly
                        m_SysPosition.Column = 0;
                        break;
                    }

                    case '\r':
                    {
                        break;
                    }

                    default:
                    {
                        m_SysPosition.Column += 1;
                        break;
                    }
                }
            }

            m_LookaheadBuffer = m_LookaheadBuffer.Remove(0, CharCount);
        }
    }

    private Token ProduceToken()
    {
        // ** VERSION 5.0 **
        // This function creates a token and also takes into account the current
        // lexing mode of the parser. In particular, it contains the group logic. 
        // 
        // A stack is used to track the current "group". This replaces the comment
        // level counter. Also, text is appended to the token on the top of the 
        // stack. This allows the group text to returned in one chunk.

        bool NestGroup;

        var Done = false;
        Token Result = null;

        while (!Done)
        {
            var Read = LookaheadDFA();

            // The logic - to determine if a group should be nested - requires that the top of the stack 
            // and the symbol's linked group need to be looked at. Both of these can be unset. So, this section
            // sets a Boolean and avoids errors. We will use this boolean in the logic chain below. 
            if (Read.Type() == SymbolType.GroupStart)
            {
                if (m_GroupStack.Count == 0)
                {
                    NestGroup = true;
                }
                else
                {
                    NestGroup = m_GroupStack.Top().Group().Nesting.Contains(Read.Group().TableIndex);
                }
            }
            else
            {
                NestGroup = false;
            }

            // =================================
            // Logic chain
            // =================================

            if (NestGroup)
            {
                ConsumeBuffer(Conversions.ToInteger(((dynamic)Read.Data).Length));
                m_GroupStack.Push(ref Read);
            }

            else if (m_GroupStack.Count == 0)
            {
                // The token is ready to be analyzed.             
                ConsumeBuffer(Conversions.ToInteger(((dynamic)Read.Data).Length));
                Result = Read;
                Done = true;
            }

            else if (ReferenceEquals(m_GroupStack.Top().Group().End, Read.Parent))
            {
                // End the current group
                var Pop = m_GroupStack.Pop();

                // === Ending logic
                if (Pop.Group().Ending == Group.EndingMode.Closed)
                {
                    Pop.Data = Pop.Data.ToString() + Read.Data; // Append text
                    ConsumeBuffer(Conversions.ToInteger(((dynamic)Read.Data).Length)); // Consume token
                }

                if (m_GroupStack.Count == 0) // We are out of the group. Return pop'd token (which contains all the group text)
                {
                    Pop.Parent = Pop.Group().Container; // Change symbol to parent
                    Result = Pop;
                    Done = true;
                }
                else
                {
                    m_GroupStack.Top().Data = m_GroupStack.Top().Data.ToString() + Pop.Data;
                } // Append group text to parent
            }

            else if (Read.Type() == SymbolType.End)
            {
                // EOF always stops the loop. The caller function (Parse) can flag a runaway group error.
                Result = Read;
                Done = true;
            }

            else
            {
                // We are in a group, Append to the Token on the top of the stack.
                // Take into account the Token group mode  
                var Top = m_GroupStack.Top();

                if (Top.Group().Advance == Group.AdvanceMode.Token)
                {
                    Top.Data = Top.Data.ToString() + Read.Data; // Append all text
                    ConsumeBuffer(Conversions.ToInteger(((dynamic)Read.Data).Length));
                }
                else
                {
                    Top.Data = Top.Data.ToString() + ((string)Read.Data)[0]; // Append one character
                    ConsumeBuffer(1);
                }
            }
        }

        return Result;
    }

    [Description(
        "Performs a parse action on the input. This method is typically used in a loop until either grammar is accepted or an error occurs.")]
    public ParseMessage Parse()
    {
        var Message = default(ParseMessage);
        Token Read;

        if (!m_TablesLoaded)
        {
            return ParseMessage.NotLoadedError;
        }

        // ===================================
        // Loop until breakable event
        // ===================================
        var Done = false;
        while (!Done)
        {
            if (m_InputTokens.Count == 0)
            {
                Read = ProduceToken();
                m_InputTokens.Push(Read);

                Message = ParseMessage.TokenRead;
                Done = true;
            }
            else
            {
                Read = m_InputTokens.Top();
                m_CurrentPosition.Copy(Read.Position()); // Update current position

                if (m_GroupStack.Count != 0) // Runaway group
                {
                    Message = ParseMessage.GroupError;
                    Done = true;
                }
                else if (Read.Type() == SymbolType.Noise)
                {
                    // Just discard. These were already reported to the user.
                    m_InputTokens.Pop();
                }

                else if (Read.Type() == SymbolType.Error)
                {
                    Message = ParseMessage.LexicalError;
                    Done = true;
                }

                else // Finally, we can parse the token.
                {
                    var Action = ParseLALR(ref Read); // SAME PROCEDURE AS v1

                    switch (Action)
                    {
                        case ParseResult.Accept:
                        {
                            Message = ParseMessage.Accept;
                            Done = true;
                            break;
                        }

                        case ParseResult.InternalError:
                        {
                            Message = ParseMessage.InternalError;
                            Done = true;
                            break;
                        }

                        case ParseResult.ReduceNormal:
                        {
                            Message = ParseMessage.Reduction;
                            Done = true;
                            break;
                        }

                        case ParseResult.Shift:
                        {
                            // ParseToken() shifted the token on the front of the Token-Queue. 
                            // It now exists on the Token-Stack and must be eliminated from the queue.
                            m_InputTokens.Dequeue();
                            break;
                        }

                        case ParseResult.SyntaxError:
                        {
                            Message = ParseMessage.SyntaxError;
                            Done = true;
                            break;
                        }

                        // Do nothing.
                    }
                }
            }
        }

        return Message;
    }


    // ===== The ParseLALR() function returns this value
    private enum ParseResult
    {
        Accept = 1,
        Shift = 2,
        ReduceNormal = 3,
        ReduceEliminated = 4, // Trim
        SyntaxError = 5,
        InternalError = 6
    }
}