using Common;
using GUI.Common;

namespace GUI.Gui;

public static class TransactionScopeHelper
{
    public static void StartTransaction()
    {
        SharedGui.ScopeStack.Push(SharedGui.MyVisioApp.BeginUndoScope(UniqueNameGenerator.GenerateUniqueName("TransactionScope")));
    }

    public static void EndTransaction(bool commit)
    {
        if (SharedGui.ScopeStack.Count == 0)
        {
            return;
        }

        SharedGui.MyVisioApp.EndUndoScope(SharedGui.ScopeStack.Pop(), commit);
    }
}