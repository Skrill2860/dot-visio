using System;
using System.Collections.Generic;
using System.Drawing;
using GUI.Common;
using GUI.Gui;
using GUI.Gui.Forms;
using Microsoft.VisualBasic;

namespace GUI.Error_Handling;

public static class WarningDialogHelper
{
    private static readonly Dictionary<string, int> ActiveWarnings = new();

    public static void ShowWarning(string msg)
    {
        try
        {
            if (ActiveWarnings.ContainsKey(msg))
            {
                return;
            }

            ActiveWarnings.Add(msg, 0);

            if (SharedGui.ActiveMessageWindow is null)
            {
                SharedGui.ActiveMessageWindow = new Messages();
            }

            SharedGui.ActiveMessageWindow.Display(msg, Color.Black);
        }
        catch (Exception ex)
        {
            Interaction.MsgBox("Unable to display message window: " + ex.Message, MsgBoxStyle.Critical);
            Interaction.MsgBox(msg, MsgBoxStyle.Exclamation);
        }
    }

    public static void ClearActiveWarnings()
    {
        ActiveWarnings.Clear();
    }
}