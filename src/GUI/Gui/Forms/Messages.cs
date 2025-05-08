using System;
using System.Drawing;
using System.Windows.Forms;
using GUI.Common;
using GUI.Error_Handling;
using static System.Windows.Forms.Application;

namespace GUI.Gui.Forms;

public partial class Messages
{
    public Messages()
    {
        InitializeComponent();
    }

    private void Messages_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing) // User trying to close, just hide
        {
            e.Cancel = true;
            Visible = false;
        }

        SharedGui.ActiveMessageWindow = null;
        WarningDialogHelper.ClearActiveWarnings();
    }

    public void ClearMessages()
    {
        TextBox.Text = "";
    }

    public void Display(string msg, Color color)
    {
        if (!string.IsNullOrEmpty(msg))
        {
            TextBox.SelectionColor = color;
            TextBox.SelectedText = msg + "\r";
        }

        Visible = true;
        DoEvents();
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
        Close();
    }
}