using System;
using System.Diagnostics;
using GUI.Error_Handling;

namespace GUI.Gui.Forms;

public partial class FatalError
{
    public FatalError(string msg)
    {
        InitializeComponent();
        txtDetails.Text = msg;
    }

    private void btnclose_Click(object sender, EventArgs e)
    {
        Close();
    }
}