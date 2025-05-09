using System;
using System.Drawing;
using System.Globalization;
using GUI.Common;
using GUI.Properties;

namespace GUI.Gui.Forms
{
    public partial class FatalError : System.Windows.Forms.Form
    {

        // Form overrides dispose to clean up the component list.
        [System.Diagnostics.DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnclose = new System.Windows.Forms.Button();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.txtDetails = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnclose
            // 
            this.btnclose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnclose.Location = new System.Drawing.Point(268, 51);
            this.btnclose.Name = "btnclose";
            this.btnclose.Size = new System.Drawing.Size(56, 24);
            this.btnclose.TabIndex = 2;
            this.btnclose.Text = "&Close";
            this.btnclose.UseVisualStyleBackColor = true;
            this.btnclose.Click += new System.EventHandler(this.btnclose_Click);
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(12, 9);
            this.Label1.MaximumSize = new System.Drawing.Size(300, 200);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(298, 39);
            this.Label1.TabIndex = 5;
            this.Label1.Text = Resources.ResourceManager.GetString(
                "FatalErrorMessageText",
                CultureInfo.GetCultureInfo(Settings.Default.ExtensionLanguage));
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(14, 62);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(65, 13);
            this.Label2.TabIndex = 16;
            this.Label2.Text = "Error details:";
            // 
            // txtDetails
            // 
            this.txtDetails.BackColor = System.Drawing.SystemColors.ControlLight;
            this.txtDetails.Location = new System.Drawing.Point(14, 83);
            this.txtDetails.Multiline = true;
            this.txtDetails.Name = "txtDetails";
            this.txtDetails.ReadOnly = true;
            this.txtDetails.Size = new System.Drawing.Size(310, 183);
            this.txtDetails.TabIndex = 15;
            // 
            // FatalError
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(336, 278);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.txtDetails);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btnclose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Location = new System.Drawing.Point(15, 15);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FatalError";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        public System.Windows.Forms.Button btnclose;
        public System.Windows.Forms.Label Label1;
        public System.Windows.Forms.Label Label2;
        public System.Windows.Forms.TextBox txtDetails;
    }
}