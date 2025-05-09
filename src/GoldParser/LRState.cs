using System.Collections;

namespace GoldParser;


public enum LrActionType
{
    Shift = 1, // Shift a symbol and goto a state
    Reduce = 2, // Reduce by a specified rule
    Goto = 3, // Goto to a state on reduction
    Accept = 4, // Input successfully parsed
    Error = 5
}

public class LrAction
{
    public readonly Symbol Symbol;
    public readonly LrActionType Type;
    public readonly short Value; // shift to state, reduce rule, goto state

    public LrAction(Symbol theSymbol, LrActionType type, short value)
    {
        Symbol = theSymbol;
        Type = type;
        Value = value;
    }
}

public class LrState : ArrayList
{
    public LrAction this[short index]
    {
        get => (LrAction)base[index]!;
        set => base[index] = value;
    }

    public LrAction this[Symbol sym]
    {
        get
        {
            int index = IndexOf(sym);
            if (index != -1)
            {
                return (LrAction)base[index]!;
            }

            return null!;
        }
        set
        {
            int index = IndexOf(sym);
            if (index != -1)
            {
                base[index] = value;
            }
        }
    }

    public short IndexOf(Symbol item)
    {
        // Returns the index of SymbolIndex in the table, -1 if not found
        short index = 0;
        short n = 0;
        var found = false;
        while (!found & (n < base.Count))
        {
            if (item.Equals(this[n].Symbol))
            {
                index = n;
                found = true;
            }

            n = (short)(n + 1);
        }

        if (found)
        {
            return index;
        }

        return -1;
    }

    public void Add(LrAction action)
    {
        base.Add(action);
    }
}

public class LrStateList : ArrayList
{
    public short InitialState;

    public LrStateList()
    {
        InitialState = 0;
    }

    public LrStateList(int size)
    {
        ReDimension(size);
        InitialState = 0;
    }

    public new LrState this[int index]
    {
        get => (LrState)base[index]!;

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

    public int Add(ref LrState item)
    {
        return base.Add(item);
    }
}