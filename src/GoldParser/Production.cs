using System.Collections;
using System.ComponentModel;

namespace GoldParser;

public class Production
{
    private readonly SymbolList m_Handle;
    // ================================================================================
    // Class Name:
    // Production 
    // 
    // Instancing:
    // Public; Non-creatable  (VB Setting: 2- PublicNotCreatable)
    // 
    // Purpose:
    // The Rule class is used to represent the logical structures of the grammar.
    // Rules consist of a head containing a nonterminal followed by a series of
    // both nonterminals and terminals.
    // 
    // Author(s):
    // Devin Cook
    // http://www.devincook.com/goldparser
    // 
    // Dependacies:
    // Symbol Class, SymbolList Class
    // 
    // ================================================================================

    private readonly Symbol m_Head;
    private readonly short m_TableIndex;

    public Production(Symbol Head, short TableIndex)
    {
        m_Head = Head;
        m_Handle = new SymbolList();
        m_TableIndex = TableIndex;
    }

    public Production()
    {
        // Nothing
    }

    [Description("Returns the head of the production.")]
    public Symbol Head()
    {
        return m_Head;
    }

    [Description("Returns the symbol list containing the handle (body) of the production.")]
    public SymbolList Handle()
    {
        return m_Handle;
    }

    [Description("Returns the index of the production in the Production Table.")]
    public short TableIndex()
    {
        return m_TableIndex;
    }

    public override string ToString()
    {
        return Text();
    }

    [Description("Returns the production in BNF.")]
    public string Text(bool AlwaysDelimitTerminals = false)
    {
        return m_Head.Text() + " ::= " + m_Handle.Text(" ", AlwaysDelimitTerminals);
    }

    public bool ContainsOneNonTerminal()
    {
        var Result = false;

        if (m_Handle.Count() == 1)
        {
            if (m_Handle[0].Type == SymbolType.Nonterminal)
            {
                Result = true;
            }
        }

        return Result;
    }
}

public class ProductionList
{
    private readonly ArrayList m_Array; // Cannot inherit, must hide methods that change the list

    public ProductionList()
    {
        m_Array = new ArrayList();
    }

    public ProductionList(int Size)
    {
        m_Array = new ArrayList();
        ReDimension(Size);
    }

    [Description("Returns the production with the specified index.")]
    public new Production this[int Index]
    {
        get => (Production)m_Array[Index];

        set => m_Array[Index] = value;
    }

    public void Clear()
    {
        m_Array.Clear();
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

    [Description("Returns the total number of productions in the list.")]
    public int Count()
    {
        return m_Array.Count;
    }

    public new int Add(Production Item)
    {
        return m_Array.Add(Item);
    }
}