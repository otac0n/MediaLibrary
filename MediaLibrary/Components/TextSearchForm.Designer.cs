namespace MediaLibrary.Components
{
    partial class TextSearchForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SearchText = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.FindPreviousButton = new System.Windows.Forms.Button();
            this.FindNextButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SearchText
            // 
            this.SearchText.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.SearchText.HideSelection = false;
            this.SearchText.Location = new System.Drawing.Point(12, 12);
            this.SearchText.Name = "SearchText";
            this.SearchText.PlaceholderText = "Search...";
            this.SearchText.Size = new System.Drawing.Size(454, 31);
            this.SearchText.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.flowLayoutPanel1.Controls.Add(this.FindPreviousButton);
            this.flowLayoutPanel1.Controls.Add(this.FindNextButton);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 63);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(454, 44);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // FindPreviousButton
            // 
            this.FindPreviousButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.FindPreviousButton.AutoSize = true;
            this.FindPreviousButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.FindPreviousButton.Location = new System.Drawing.Point(323, 3);
            this.FindPreviousButton.Name = "FindPreviousButton";
            this.FindPreviousButton.Size = new System.Drawing.Size(128, 35);
            this.FindPreviousButton.TabIndex = 2;
            this.FindPreviousButton.Text = "Find Previous";
            this.FindPreviousButton.UseVisualStyleBackColor = true;
            this.FindPreviousButton.Click += this.FindPreviousButton_Click;
            // 
            // FindNextButton
            // 
            this.FindNextButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.FindNextButton.AutoSize = true;
            this.FindNextButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.FindNextButton.Location = new System.Drawing.Point(220, 3);
            this.FindNextButton.Name = "FindNextButton";
            this.FindNextButton.Size = new System.Drawing.Size(97, 35);
            this.FindNextButton.TabIndex = 1;
            this.FindNextButton.Text = "Find Next";
            this.FindNextButton.UseVisualStyleBackColor = true;
            this.FindNextButton.Click += this.FindNextButton_Click;
            // 
            // TextSearchForm
            // 
            this.AcceptButton = this.FindNextButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 119);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.SearchText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 175);
            this.Name = "TextSearchForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Find";
            this.FormClosing += this.TextSearchForm_FormClosing;
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox SearchText;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button FindPreviousButton;
        private System.Windows.Forms.Button FindNextButton;
    }
}