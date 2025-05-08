using System;
using System.Threading;
using Common;
using GUI.Error_Handling;
using GUI.Gui;
using GUI.Properties;

namespace GUI.Common;

public static class ProgressBarRunner
{
    public static Thread? ProgressTaskThread;
    private static readonly object CurrentTaskLock = new();

    public static bool CanRun()
    {
        lock (CurrentTaskLock)
        {
            return ProgressTaskThread == null;
        }
    }

    public static void Run(Action action, bool asynchronous)
    {
        lock (CurrentTaskLock)
        {
            if (ProgressTaskThread != null)
            {
                WarningDialogHelper.ShowWarning(Resources.ResourceManager.GetString("AnotherProcessAlreadyRunning") ??
                                                "Another process already running");

                return;
            }

            SharedGui.MyVisioApp.ScreenUpdating = (short)(Settings.Default.UpdateWhileDrawing ? 1 : 0);

            if (asynchronous)
            {
                ProgressTaskThread = new Thread(() => DoWork(action))
                {
                    Priority = ThreadPriority.AboveNormal
                };
                ProgressTaskThread.SetApartmentState(ApartmentState.STA);

                ProgressTaskThread.Start();
            }
            else
            {
                DoWork(action);
            }
        }
    }

    private static void DoWork(Action action)
    {
        try
        {
            ProgressHelper.StartProgress("Launching " + action.Method.Name + " task...", 0);
            TransactionScopeHelper.StartTransaction();
            action();
            TransactionScopeHelper.EndTransaction(SharedConstants.COMMIT);
            ProgressHelper.EndProgress();
        }
        catch (ThreadAbortException)
        {
            Thread.ResetAbort();
            TransactionScopeHelper.EndTransaction(SharedConstants.ROLLBACK);
            WarningDialogHelper.ShowWarning("Task cancelled.");
            ProgressHelper.EndProgress(true);
        }
        catch (Exception ex)
        {
            TransactionScopeHelper.EndTransaction(SharedConstants.ROLLBACK);
            ErrorDialogHelper.HandleError(ex);
            ProgressHelper.EndProgress(true);
        }
        finally
        {
            lock (CurrentTaskLock)
            {
                ProgressTaskThread = null;

                SharedGui.MyVisioApp.ScreenUpdating = 1;
                while (SharedGui.ActiveMessageWindow != null)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }
    }
}