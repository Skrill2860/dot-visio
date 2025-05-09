using System.Collections;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GoldParser;

public enum SymbolType
{
    Nonterminal = 0, // Nonterminal 
    Content = 1, // Passed to the parser
    Noise = 2, // Ignored by the parser
    End = 3, // End character (EOF)
    GroupStart = 4, // Group start  
    GroupEnd = 5, // Group end   

    // Note: There is no value 6. CommentLine was deprecated.
    Error = 7 // Error symbol
}

public class Symbol
{
    // ================================================================================
    // Class Name:
    // Symbol
    // 
    // Purpose:
    // This class is used to store of the nonterminals used by the Deterministic
    // Finite Automata (DFA) and LALR Parser. Symbols can be either
    // terminals (which represent a class of tokens - such as identifiers) or
    // nonterminals (which represent the rules and structures of the grammar).
    // Terminal symbols fall into several catagories for use by the GOLD Parser
    // Engine which are enumerated below.
    // 
    // Author(s):
    // Devin Cook
    // 
    // Dependacies:
    // (None)
    // 
    // ================================================================================

    private readonly string m_Name;
    private readonly short m_TableIndex;
    public Group Group;

    public Symbol()
    {
        // Nothing
    }

    public Symbol(string Name, SymbolType Type, short TableIndex)
    {
        m_Name = Name;
        this.Type = Type;
        m_TableIndex = TableIndex;
    }

    [Description("Returns the type of the symbol.")]
    public SymbolType Type { get; set; }

    [Description("Returns the index of the symbol in the Symbol Table,")]
    public short TableIndex()
    {
        return m_TableIndex;
    }

    [Description("Returns the name of the symbol.")]
    public string Name()
    {
        return m_Name;
    }

    [Description("Returns the text representing the text in BNF format.")]
    public string Text(bool AlwaysDelimitTerminals)
    {
        string Result;

        switch (Type)
        {
            case SymbolType.Nonterminal:
            {
                Result = "<" + Name() + ">";
                break;
            }
            case SymbolType.Content:
            {
                Result = LiteralFormat(Name(), AlwaysDelimitTerminals);
                break;
            }

            default:
            {
                Result = "(" + Name() + ")";
                break;
            }
        }

        return Result;
    }

    [Description("Returns the text representing the text in BNF format.")]
    public string Text()
    {
        return Text(false);
    }

    private string LiteralFormat(string Source, bool ForceDelimit)
    {
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(Source, "'",
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
        {
            return "''";
        }

        short n = 0;
        while ((n < Source.Length) & !ForceDelimit)
        {
            var ch = Source[n];
            ForceDelimit = !(char.IsLetter(ch) | (Conversions.ToString(ch) == ".") | (Conversions.ToString(ch) == "_") |
                             (Conversions.ToString(ch) == "-"));
            n = (short)(n + 1);
        }

        if (ForceDelimit)
        {
            return "'" + Source + "'";
        }

        return Source;
    }

    public override string ToString()
    {
        return Text();
    }
}

public class SymbolList
{
    private readonly ArrayList m_Array; // CANNOT inherit, must hide methods that edit the list

    public SymbolList()
    {
        m_Array = new ArrayList();
    }

    public SymbolList(int Size)
    {
        m_Array = new ArrayList();
        ReDimension(Size);
    }

    [Description("Returns the symbol with the specified index.")]
    public Symbol this[int Index]
    {
        get
        {
            if ((Index >= 0) & (Index < m_Array.Count))
            {
                return (Symbol)m_Array[Index];
            }

            return null;
        }

        set => m_Array[Index] = value;
    }

    public void ReDimension(int Size)
    {
        // Increase the size of the array to Size empty elements.

        m_Array.Clear();
        var loopTo = Size - 1;
        for (var n = 0; n <= loopTo; n++)
        {
            m_Array.Add(null);
        }
    }

    [Description("Returns the total number of symbols in the list.")]
    public int Count()
    {
        return m_Array.Count;
    }

    public void Clear()
    {
        m_Array.Clear();
    }

    public int Add(Symbol Item)
    {
        return m_Array.Add(Item);
    }

    public Symbol GetFirstOfType(SymbolType Type)
    {
        Symbol Result = null;

        var Found = false;
        short n = 0;
        while (!Found & (n < m_Array.Count))
        {
            var Sym = (Symbol)m_Array[n];
            if (Sym.Type == Type)
            {
                Found = true;
                Result = Sym;
            }

            n = (short)(n + 1);
        }

        return Result;
    }

    public override string ToString()
    {
        return Text();
    }

    [Description("Returns a list of the symbol names in BNF format.")]
    public string Text(string Separator, bool AlwaysDelimitTerminals)
    {
        var Result = "";

        var loopTo = m_Array.Count - 1;
        for (var n = 0; n <= loopTo; n++)
        {
            var Sym = (Symbol)m_Array[n];
            Result = Conversions.ToString(Result +
                                          Operators.ConcatenateObject(Interaction.IIf(n == 0, "", Separator),
                                              Sym.Text(AlwaysDelimitTerminals)));
        }

        return Result;
    }

    [Description("Returns a list of the symbol names in BNF format.")]
    public string Text()
    {
        return Text(", ", false);
    }
}