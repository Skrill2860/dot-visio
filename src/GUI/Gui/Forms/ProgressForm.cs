using System;
using System.Windows.Forms;
using GUI.Common;

namespace GUI.Gui.Forms;

public partial class ProgressForm
{
    private void ProgressForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        SharedGui.ProgressWindow = null;
    }

    private void ProgressForm_Load(object sender, EventArgs e)
    {
        var screen = Screen.PrimaryScreen;
        Top = (int)Math.Round(screen.Bounds.Height / 3d);
    }

    private void _CancelButton_Click(object sender, EventArgs e)
    {
        try
        {
            CancelButton.Enabled = false;
            _Status.Text = "Cancelling...";
            Refresh();
            ProgressBarRunner.ProgressTaskThread?.Abort();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void _CancelButton_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Escape)
        {
            _CancelButton_Click(sender, e);
        }
    }
}