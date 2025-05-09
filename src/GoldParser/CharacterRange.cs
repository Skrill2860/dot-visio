using System.Collections;

namespace GoldParser;

public class CharacterRange
{
    public readonly ushort End;
    public readonly ushort Start;

    public CharacterRange(ushort start, ushort end)
    {
        Start = start;
        End = end;
    }
}

public class CharacterSet : ArrayList
{
    public new CharacterRange this[int index]
    {
        get => (CharacterRange)base[index]!;

        set => base[index] = value;
    }

    public int Add(ref CharacterRange item)
    {
        return base.Add(item);
    }

    public bool Contains(int charCode)
    {
        // This procedure searchs the set to deterimine if the CharCode is in one
        // of the ranges - and, therefore, the set.
        // The number of ranges in any given set are relatively small - rarely 
        // exceeding 10 total. As a result, a simple linear search is sufficient 
        // rather than a binary search. In fact, a binary search overhead might
        // slow down the search!

        var found = false;
        var n = 0;

        while ((n < base.Count) & !found)
        {
            var range = (CharacterRange)base[n]!;

            found = (charCode >= range.Start) & (charCode <= range.End);
            n += 1;
        }

        return found;
    }
}

public class CharacterSetList : ArrayList
{
    public CharacterSetList()
    {
    }

    public CharacterSetList(int size)
    {
        ReDimension(size);
    }

    public new CharacterSet this[int index]
    {
        get => (CharacterSet)base[index]!;

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

    public new int Add(ref CharacterSet item)
    {
        return base.Add(item);
    }
}