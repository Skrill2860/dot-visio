using System.Collections;
using System.ComponentModel;

namespace GoldParser;

// ================================================================================
// Class Name:
// Reduction
// 
// Instancing:
// Public; Creatable  (VB Setting: 2 - PublicNotCreatable)
// 
// Purpose:
// This class is used by the engine to hold a reduced rule. Rather the contain
// a list of Symbols, a reduction contains a list of Tokens corresponding to the
// the rule it represents. This class is important since it is used to store the
// actual source program parsed by the Engine.
// 
// Author(s):
// Devin Cook
// 
// Dependacies:
// ================================================================================

public class Reduction : TokenList
{
    public Reduction(int Size)
    {
        ReDimension(Size);
    }

    [Description("Returns the parent production.")]
    public Production Parent { get; set; }

    [Description("Returns/sets any additional user-defined data to this object.")]
    public object Tag { get; set; }

    public ArrayList Tokens => Items;

    public void ReDimension(int Size)
    {
        // Increase the size of the array to Size empty elements.

        Clear();
        var loopTo = Size - 1;
        for (var n = 0; n <= loopTo; n++)
        {
            Add(null);
        }
    }

    public object get_Data(int Index)
    {
        return base[Index].Data;
    }

    public void set_Data(int Index, object value)
    {
        base[Index].Data = value;
    }
}