using System.Collections;

namespace GoldParser;

public class FaState
{
    public readonly Symbol Accept;
    // ================================================================================
    // Class Name:
    // FAState
    // 
    // Purpose:
    // Represents a state in the Deterministic Finite Automata which is used by
    // the tokenizer.
    // 
    // Author(s):
    // Devin Cook
    // 
    // Dependacies:
    // FAEdge, Symbol
    // 
    // ================================================================================

    public readonly FaEdgeList Edges;

    public FaState(Symbol accept)
    {
        Accept = accept;
        Edges = new FaEdgeList();
    }

    public FaState()
    {
        Accept = null!;
        Edges = new FaEdgeList();
    }
}

public class FaStateList : ArrayList
{
    // ===== DFA runtime variables
    public Symbol ErrorSymbol;

    public short InitialState;

    public FaStateList()
    {
        InitialState = 0;
        ErrorSymbol = null!;
    }

    public FaStateList(int size)
    {
        ReDimension(size);

        InitialState = 0;
        ErrorSymbol = null!;
    }

    public new FaState this[int index]
    {
        get => (FaState)base[index]!;

        set => base[index] = value;
    }

    private void ReDimension(int size)
    {
        // Increase the size of the array to Size empty elements.

        base.Clear();
        var loopTo = size - 1;
        for (var n = 0; n <= loopTo; n++)
        {
            base.Add(null);
        }
    }

    public new int Add(FaState item)
    {
        return base.Add(item);
    }
}