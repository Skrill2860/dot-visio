using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GUI.Gui.Forms;
using Microsoft.Office.Interop.Visio;

namespace GUI.Common;

public static class SharedGui
{
    public static readonly Dictionary<int, DotSettings> PageTable = new();
    public static Messages? ActiveMessageWindow = null;
    public static ProgressForm? ProgressWindow = null;
    public static readonly Stack<int> ScopeStack = new();

    private static readonly DotSettings DefaultDotSettings = new(-1);
    public static DotSettings CurrentDotSettings = DefaultDotSettings;


    public static Application MyVisioApp
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set;
    }
}