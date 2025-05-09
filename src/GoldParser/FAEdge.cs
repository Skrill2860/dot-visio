using System.Collections;

namespace GoldParser;

public class FaEdge
{
    // ================================================================================
    // Class Name:
    // FAEdge
    // 
    // Purpose:
    // Each state in the Determinstic Finite Automata contains multiple edges which
    // link to other states in the automata.
    // 
    // This class is used to represent an edge.
    // 
    // Author(s):
    // Devin Cook
    // http://www.DevinCook.com/GOLDParser
    // 
    // Dependacies:
    // (None)
    // 
    // ================================================================================

    public readonly CharacterSet Characters; // Characters to advance on	
    public readonly int Target; // FAState

    public FaEdge(CharacterSet charSet, int target)
    {
        Characters = charSet;
        Target = target;
    }
}

public class FaEdgeList : ArrayList
{
    public new FaEdge this[int index]
    {
        get => (FaEdge)base[index]!;
        set => base[index] = value;
    }

    public new int Add(FaEdge edge)
    {
        return base.Add(edge);
    }
}