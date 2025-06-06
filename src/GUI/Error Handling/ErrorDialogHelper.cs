﻿using System;
using System.Drawing;
using System.Threading;
using Domain;
using GUI.Common;
using GUI.Gui;
using GUI.Gui.Forms;
using Microsoft.VisualBasic;

namespace GUI.Error_Handling;

public static class ErrorDialogHelper
{
    public static void HandleError(Exception ex)
    {
        SharedGui.MyVisioApp.ScreenUpdating = 1;

        if (SharedGui.ProgressWindow is not null)
        {
            try
            {
                SharedGui.ProgressWindow.Close();
                SharedGui.ProgressWindow = null;
            }
            catch
            {
                // ignored
            }
        }

        SharedGui.ActiveMessageWindow = new Messages();
        if (ex is ThreadAbortException)
        {
            SharedGui.ActiveMessageWindow.Display("Operation cancelled", Color.Red);
        }
        else if (ex is DotVisioException)
        {
            SharedGui.ActiveMessageWindow.Display(ex.Message, Color.Red);
        }
        else
        {
            var msg = ex.Message + "\r\n";
            var e = ex.InnerException;
            while (e is not null)
            {
                msg = msg + "\r\n" + "...caused by " + e.Message;
                e = e.InnerException;
            }

            msg = msg + "\r\n" + "Stack trace:" + "\r\n" + ex.StackTrace;
            var errorform = new FatalError(msg);
            errorform.ShowDialog();
        }
    }
}