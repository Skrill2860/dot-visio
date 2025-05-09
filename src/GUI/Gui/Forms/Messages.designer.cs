using System;
using System.Windows.Forms;

namespace GUI.Gui.Forms
{
    public partial class Messages : System.Windows.Forms.Form
    {

        // Form overrides dispose to clean up the component list.
        [System.Diagnostics.DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components != null)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [System.Diagnostics.DebuggerStepThrough()]
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            var resources = new System.ComponentModel.ComponentResourceManager(typeof(Messages));
            TextBox = new System.Windows.Forms.RichTextBox();
            btnOK = new System.Windows.Forms.Button();
            btnOK.Click += new EventHandler(btnOK_Click);
            SplitContainer = new System.Windows.Forms.SplitContainer();
            SplitContainer.Panel1.SuspendLayout();
            SplitContainer.Panel2.SuspendLayout();
            SplitContainer.SuspendLayout();
            SuspendLayout();
            // 
            // TextBox
            // 
            TextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            TextBox.CausesValidation = false;
            TextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            TextBox.Location = new System.Drawing.Point(0, 0);
            TextBox.Name = "TextBox";
            TextBox.ReadOnly = true;
            TextBox.ShortcutsEnabled = false;
            TextBox.Size = new System.Drawing.Size(342, 116);
            TextBox.TabIndex = 0;
            TextBox.Text = "";

            TextBox.ReadOnly = true;
            TextBox.Multiline = true;
            TextBox.WordWrap = true;
            TextBox.ScrollBars = RichTextBoxScrollBars.Vertical;

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Copy", (s, e) => { TextBox.SelectAll(); TextBox.Copy(); });
            TextBox.ContextMenu = contextMenu;
            // 
            // btnOK
            // 
            btnOK.Location = new System.Drawing.Point(154, 7);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(45, 22);
            btnOK.TabIndex = 1;
            btnOK.Text = "&OK";
            btnOK.UseVisualStyleBackColor = true;
            // 
            // SplitContainer
            // 
            SplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            SplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            SplitContainer.Location = new System.Drawing.Point(0, 0);
            SplitContainer.Name = "SplitContainer";
            SplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // SplitContainer.Panel1
            // 
            SplitContainer.Panel1.Controls.Add(TextBox);
            // 
            // SplitContainer.Panel2
            // 
            SplitContainer.Panel2.Controls.Add(btnOK);
            SplitContainer.Panel2MinSize = 35;
            SplitContainer.Size = new System.Drawing.Size(342, 155);
            SplitContainer.SplitterDistance = 116;
            SplitContainer.TabIndex = 2;
            // 
            // Messages
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6.0f, 13.0f);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(342, 155);
            Controls.Add(SplitContainer);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Messages";
            ShowInTaskbar = false;
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "DotVisio Messages";
            TopMost = true;
            SplitContainer.Panel1.ResumeLayout(false);
            SplitContainer.Panel2.ResumeLayout(false);
            SplitContainer.ResumeLayout(false);
            FormClosing += new System.Windows.Forms.FormClosingEventHandler(Messages_FormClosing);
            ResumeLayout(false);
        }

        public System.Windows.Forms.RichTextBox TextBox;
        public System.Windows.Forms.Button btnOK;
        public System.Windows.Forms.SplitContainer SplitContainer;
    }
}