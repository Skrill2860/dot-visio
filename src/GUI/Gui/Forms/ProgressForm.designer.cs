using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GUI.Gui.Forms
{
    public partial class ProgressForm : System.Windows.Forms.Form
    {

        [System.Diagnostics.DebuggerNonUserCode()]
        public ProgressForm() : base()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();
            FormClosed += ProgressForm_FormClosed;
        }

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
            this.components = new System.ComponentModel.Container();
            this._CancelButton = new System.Windows.Forms.Button();
            this._Progress = new System.Windows.Forms.ProgressBar();
            this._Status = new System.Windows.Forms.TextBox();
            this._Timer = new System.Windows.Forms.Timer(this.components);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.SuspendLayout();
            // 
            // _CancelButton
            // 
            this._CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._CancelButton.Location = new System.Drawing.Point(195, 12);
            this._CancelButton.Name = "_CancelButton";
            this._CancelButton.Size = new System.Drawing.Size(53, 24);
            this._CancelButton.TabIndex = 1;
            this._CancelButton.Text = "&Cancel";
            this._CancelButton.UseVisualStyleBackColor = true;
            this._CancelButton.Click += new System.EventHandler(this._CancelButton_Click);
            this._CancelButton.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this._CancelButton_KeyPress);
            // 
            // _Progress
            // 
            this._Progress.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._Progress.Location = new System.Drawing.Point(0, 48);
            this._Progress.Name = "_Progress";
            this._Progress.Size = new System.Drawing.Size(260, 21);
            this._Progress.Step = 1;
            this._Progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._Progress.TabIndex = 4;
            // 
            // _Status
            // 
            this._Status.BackColor = System.Drawing.SystemColors.Control;
            this._Status.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._Status.Location = new System.Drawing.Point(12, 12);
            this._Status.Multiline = true;
            this._Status.Name = "_Status";
            this._Status.Size = new System.Drawing.Size(177, 30);
            this._Status.TabIndex = 5;
            // 
            // ProgressForm
            // 
            this.ClientSize = new System.Drawing.Size(260, 69);
            this.Controls.Add(this._Status);
            this.Controls.Add(this._Progress);
            this.Controls.Add(this._CancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DotVisio progress";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button _CancelButton;
        private new System.Windows.Forms.Button CancelButton
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _CancelButton;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_CancelButton != null)
                {
                    _CancelButton.Click -= _CancelButton_Click;
                }

                _CancelButton = value;
                if (_CancelButton != null)
                {
                    _CancelButton.Click += _CancelButton_Click;
                }
            }
        }

        private System.Windows.Forms.ProgressBar _Progress;
        public virtual System.Windows.Forms.ProgressBar Progress
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _Progress;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _Progress = value;
            }
        }

        private System.Windows.Forms.TextBox _Status;
        public virtual System.Windows.Forms.TextBox Status
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _Status;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _Status = value;
            }
        }

        private System.Windows.Forms.Timer _Timer;
        public virtual System.Windows.Forms.Timer Timer
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _Timer;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _Timer = value;
            }
        }
    }
}