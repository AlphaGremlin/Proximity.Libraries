/****************************************\
 ExceptionForm.Designer.cs
 Created: 6-02-2008
\****************************************/
namespace Proximity.Gui.WinForms
{
	partial class ExceptionForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.butExit = new System.Windows.Forms.Button();
			this.butContinue = new System.Windows.Forms.Button();
			this.lblInfo = new System.Windows.Forms.Label();
			this.txtException = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// butExit
			// 
			this.butExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.butExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.butExit.Location = new System.Drawing.Point(323, 403);
			this.butExit.Name = "butExit";
			this.butExit.Size = new System.Drawing.Size(96, 22);
			this.butExit.TabIndex = 5;
			this.butExit.Text = "&Exit";
			this.butExit.UseVisualStyleBackColor = true;
			// 
			// butContinue
			// 
			this.butContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.butContinue.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.butContinue.Location = new System.Drawing.Point(221, 403);
			this.butContinue.Name = "butContinue";
			this.butContinue.Size = new System.Drawing.Size(96, 22);
			this.butContinue.TabIndex = 4;
			this.butContinue.Text = "&Continue";
			this.butContinue.UseVisualStyleBackColor = true;
			// 
			// lblInfo
			// 
			this.lblInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.lblInfo.Location = new System.Drawing.Point(12, 9);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(407, 17);
			this.lblInfo.TabIndex = 6;
			this.lblInfo.Text = "An unexpected exception has occurred. Read on for more information...";
			// 
			// txtException
			// 
			this.txtException.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.txtException.Location = new System.Drawing.Point(12, 29);
			this.txtException.Multiline = true;
			this.txtException.Name = "txtException";
			this.txtException.ReadOnly = true;
			this.txtException.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtException.Size = new System.Drawing.Size(407, 368);
			this.txtException.TabIndex = 7;
			// 
			// ExceptionForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.butExit;
			this.ClientSize = new System.Drawing.Size(431, 437);
			this.Controls.Add(this.butExit);
			this.Controls.Add(this.butContinue);
			this.Controls.Add(this.lblInfo);
			this.Controls.Add(this.txtException);
			this.MinimizeBox = false;
			this.Name = "ExceptionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Black Fusion Engine Exception";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.TextBox txtException;
		private System.Windows.Forms.Label lblInfo;
		private System.Windows.Forms.Button butContinue;
		private System.Windows.Forms.Button butExit;
	}
}
