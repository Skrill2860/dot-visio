using System.Windows.Forms;
using GUI.Common;
using GUI.Gui.Forms;
using static System.Windows.Forms.Application;

namespace GUI.Gui;

public static class ProgressHelper
{
    private static int _depthOfSubProgresses;
    private static readonly object ProgressLock = new();

    public static void StartProgress(string msg, int max)
    {
        lock (ProgressLock)
        {
            if (SharedGui.ProgressWindow != null && SharedGui.ProgressWindow.InvokeRequired)
            {
                SharedGui.ProgressWindow.Invoke(StartProgress, msg, max);
            }
            else
            {
                if (SharedGui.ProgressWindow is null || _depthOfSubProgresses == 0)
                {
                    SharedGui.ProgressWindow = new ProgressForm();
                }

                _depthOfSubProgresses += 1;
                SharedGui.ProgressWindow.Status.Text = msg;
                SharedGui.ProgressWindow.Progress.Value = 0;
                SharedGui.ProgressWindow.Progress.Style = max <= 0 ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                SharedGui.ProgressWindow.Progress.Maximum = max < 0 ? 0 : max;
                SharedGui.ProgressWindow.Visible = true;
            }
        }

        DoEvents();
    }

    public static void IncreaseProgress()
    {
        lock (ProgressLock)
        {
            if (SharedGui.ProgressWindow is null)
            {
                return;
            }

            if (SharedGui.ProgressWindow.InvokeRequired)
            {
                SharedGui.ProgressWindow.Invoke(IncreaseProgress);
            }
            else
            {
                if (SharedGui.ProgressWindow.Progress.Value < SharedGui.ProgressWindow.Progress.Maximum)
                {
                    SharedGui.ProgressWindow.Progress.PerformStep();
                }

                DoEvents();
            }
        }
    }

    public static void EndProgress(bool force = false)
    {
        lock (ProgressLock)
        {
            _depthOfSubProgresses -= 1;
            if (_depthOfSubProgresses > 0 && !force)
            {
                return;
            }

            if (SharedGui.ProgressWindow != null)
            {
                if (SharedGui.ProgressWindow.InvokeRequired)
                {
                    SharedGui.ProgressWindow.Invoke(SharedGui.ProgressWindow.Close);
                }
                else
                {
                    SharedGui.ProgressWindow.Close();
                }
            }

            _depthOfSubProgresses = 0;
        }
    }
}