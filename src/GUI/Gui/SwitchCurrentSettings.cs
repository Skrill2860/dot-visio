using GUI.Common;
using Microsoft.Office.Interop.Visio;

namespace GUI.Gui;

public static class _SwitchCurrentSettings
{
    public static void SwitchCurrentSettings()
    {
        var setId = -1;

        if (SharedGui.MyVisioApp.ActiveDocument != null && SharedGui.MyVisioApp.ActiveDocument.Type != VisDocumentTypes.visTypeDrawing)
        {
            return; // Forget about it when stencils are activated
        }

        var doc = SharedGui.MyVisioApp.ActiveDocument;

        if (doc != null)
        {
            setId = doc.ID;
        }

        if (setId == SharedGui.CurrentDotSettings.Id)
        {
            SharedGui.CurrentDotSettings.SaveToActiveDocument();
        }

        if (SharedGui.PageTable.TryGetValue(setId, out SharedGui.CurrentDotSettings))
        {
            return;
        }

        SharedGui.CurrentDotSettings = new DotSettings(setId);
        SharedGui.CurrentDotSettings.InitFromActiveDocument();
    }
}