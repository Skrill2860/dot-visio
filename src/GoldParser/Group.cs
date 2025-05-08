using System.Collections;
using Microsoft.VisualBasic.CompilerServices;

namespace GoldParser;

public class Group
{
    public enum AdvanceMode
    {
        Token = 0,
        Character = 1
    }

    public enum EndingMode
    {
        Open = 0,
        Closed = 1
    }

    public readonly IntegerList Nesting;

    public AdvanceMode Advance;
    public Symbol Container;
    public Symbol End;
    public EndingMode Ending;

    public string Name;
    public Symbol Start;

    public short TableIndex;

    public Group()
    {
        Advance = AdvanceMode.Character;
        Ending = EndingMode.Closed;
        Nesting = new IntegerList(); // GroupList
    }
}

public class GroupList : ArrayList
{
    public GroupList()
    {
    }

    public GroupList(int size)
    {
        ReDimension(size);
    }

    public new Group this[int index]
    {
        get => (Group)base[index]!;

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

    public new int Add(Group item)
    {
        return base.Add(item);
    }
}

public class IntegerList : ArrayList
{
    public new int this[int index]
    {
        get => Conversions.ToInteger(base[index]);

        set => base[index] = value;
    }

    public new int Add(int value)
    {
        return base.Add(value);
    }

    public new bool Contains(int item)
    {
        return base.Contains(item);
    }
}