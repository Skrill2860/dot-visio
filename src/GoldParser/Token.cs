using System.Collections;
using System.ComponentModel;

namespace GoldParser;

public class Token
{
    private readonly Position m_Position = new();

    // ================================================================================
    // Class Name:
    // Token
    // 
    // Purpose:
    // While the Symbol represents a class of terminals and nonterminals, the
    // Token represents an individual piece of information.
    // Ideally, the token would inherit directly from the Symbol Class, but do to
    // the fact that Visual Basic 5/6 does not support this aspect of Object Oriented
    // Programming, a Symbol is created as a member and its methods are mimicked.
    // 
    // Author(s):
    // Devin Cook
    // 
    // Dependencies:
    // Symbol, Position
    // 
    // ================================================================================

    public Token()
    {
        Parent = null;
        Data = null;
        State = 0;
    }

    public Token(Symbol Parent, object Data)
    {
        this.Parent = Parent;
        this.Data = Data;
        State = 0;
    }

    [Description("Returns/sets the object associated with the token.")]
    public object Data { get; set; }

    public short State { get; set; }

    [Description("Returns the parent symbol of the token.")]
    public Symbol Parent { get; set; }

    [Description("Returns the line/column position where the token was read.")]
    public Position Position()
    {
        return m_Position;
    }

    [Description("Returns the symbol type associated with this token.")]
    public SymbolType Type()
    {
        return Parent.Type;
    }

    public Group Group()
    {
        return Parent.Group;
    }
}

public class TokenList
{
    public TokenList()
    {
        Items = new ArrayList();
    }

    [Description("Returns the token with the specified index.")]
    public new Token this[int Index]
    {
        get => (Token)Items[Index];

        set => Items[Index] = value;
    }

    public ArrayList Items { get; }

    public int Add(Token Item)
    {
        return Items.Add(Item);
    }

    [Description("Returns the total number of tokens in the list.")]
    public int Count()
    {
        return Items.Count;
    }

    public void Clear()
    {
        Items.Clear();
    }
}

public class TokenStack
{
    // ================================================================================
    // Class Name:
    // TokenStack    '
    // Instancing:
    // Private; Internal  (VB Setting: 1 - Private)
    // 
    // Purpose:
    // This class is used by the GOLDParser class to store tokens during parsing.
    // In particular, this class is used the the LALR(1) state machine.
    // 
    // Author(s):
    // Devin Cook
    // GOLDParser@DevinCook.com
    // 
    // Dependacies:
    // Token Class
    // 
    // Revision History
    // 12/11/2001
    // Modified the stack to not deallocate the array until cleared
    // ================================================================================

    private readonly Stack m_Stack;

    public TokenStack()
    {
        m_Stack = new Stack();
    }

    public int Count => m_Stack.Count;

    public void Clear()
    {
        m_Stack.Clear();
    }

    public void Push(ref Token TheToken)
    {
        m_Stack.Push(TheToken);
    }

    public Token Pop()
    {
        return (Token)m_Stack.Pop();
    }

    public Token Top()
    {
        return (Token)m_Stack.Peek();
    }
}

public class TokenQueueStack
{
    private readonly ArrayList m_Items;

    public TokenQueueStack()
    {
        m_Items = new ArrayList();
    }

    public int Count => m_Items.Count;

    public void Clear()
    {
        m_Items.Clear();
    }

    public void Enqueue(ref Token TheToken)
    {
        m_Items.Add(TheToken); // End of list
    }

    public Token Dequeue()
    {
        var Result = (Token)m_Items[0]; // Front of list
        m_Items.RemoveAt(0);

        return Result;
    }

    public Token Top()
    {
        if (m_Items.Count >= 1)
        {
            return (Token)m_Items[0];
        }

        return null;
    }

    public void Push(Token TheToken)
    {
        m_Items.Insert(0, TheToken);
    }

    public Token Pop()
    {
        return Dequeue(); // Same as dequeue
    }
}