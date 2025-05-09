using System;
using DotVisio.Ribbons;
using GUI.Common;
using Microsoft.Office.Core;

namespace DotVisio;

public partial class DotVisioAddIn
{
    private void DotVisioAddIn_Startup(object sender, EventArgs e)
    {
        SharedGui.MyVisioApp = Application;
    }

    private void DotVisioAddIn_Shutdown(object sender, EventArgs e)
    {
    }

    protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
    {
        return new DotVisioRibbon();
    }

    /// <summary>
    ///     Required method for Designer support - do not modify
    ///     the contents of this method with the code editor.
    /// </summary>
    private void InternalStartup()
    {
        Startup += DotVisioAddIn_Startup;
        Shutdown += DotVisioAddIn_Shutdown;
    }
}