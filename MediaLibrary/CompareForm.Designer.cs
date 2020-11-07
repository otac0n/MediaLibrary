namespace MediaLibrary
{
    partial class CompareForm
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
            this.previewTable = new System.Windows.Forms.TableLayoutPanel();
            this.rateButton = new System.Windows.Forms.Button();
            this.skipButton = new System.Windows.Forms.Button();
            this.controlLayoutTable = new System.Windows.Forms.TableLayoutPanel();
            this.ratingBar = new System.Windows.Forms.TrackBar();
            this.previewTable.SuspendLayout();
            this.controlLayoutTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ratingBar)).BeginInit();
            this.SuspendLayout();
            // 
            // previewTable
            // 
            this.previewTable.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.previewTable.ColumnCount = 2;
            this.previewTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.previewTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.previewTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewTable.Location = new System.Drawing.Point(0, 0);
            this.previewTable.Name = "previewTable";
            this.previewTable.RowCount = 1;
            this.previewTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewTable.Size = new System.Drawing.Size(800, 370);
            this.previewTable.TabIndex = 3;
            // 
            // rateButton
            // 
            this.rateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rateButton.Location = new System.Drawing.Point(322, 54);
            this.rateButton.Name = "rateButton";
            this.rateButton.Size = new System.Drawing.Size(75, 23);
            this.rateButton.TabIndex = 1;
            this.rateButton.Text = "Rate";
            this.rateButton.UseVisualStyleBackColor = true;
            this.rateButton.Click += new System.EventHandler(this.RateButton_Click);
            // 
            // skipButton
            // 
            this.skipButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.skipButton.Location = new System.Drawing.Point(403, 54);
            this.skipButton.Name = "skipButton";
            this.skipButton.Size = new System.Drawing.Size(75, 23);
            this.skipButton.TabIndex = 2;
            this.skipButton.Text = "Skip";
            this.skipButton.UseVisualStyleBackColor = true;
            this.skipButton.Click += new System.EventHandler(this.SkipButton_Click);
            // 
            // controlLayoutTable
            // 
            this.controlLayoutTable.AutoSize = true;
            this.controlLayoutTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.controlLayoutTable.ColumnCount = 4;
            this.controlLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.controlLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.controlLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.controlLayoutTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.controlLayoutTable.Controls.Add(this.skipButton, 2, 1);
            this.controlLayoutTable.Controls.Add(this.rateButton, 1, 1);
            this.controlLayoutTable.Controls.Add(this.ratingBar, 1, 0);
            this.controlLayoutTable.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.controlLayoutTable.Location = new System.Drawing.Point(0, 370);
            this.controlLayoutTable.Margin = new System.Windows.Forms.Padding(0);
            this.controlLayoutTable.Name = "controlLayoutTable";
            this.controlLayoutTable.RowCount = 2;
            this.controlLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.controlLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.controlLayoutTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.controlLayoutTable.Size = new System.Drawing.Size(800, 80);
            this.controlLayoutTable.TabIndex = 4;
            // 
            // ratingBar
            // 
            this.controlLayoutTable.SetColumnSpan(this.ratingBar, 2);
            this.ratingBar.LargeChange = 1;
            this.ratingBar.Location = new System.Drawing.Point(228, 3);
            this.ratingBar.Name = "ratingBar";
            this.ratingBar.Size = new System.Drawing.Size(344, 45);
            this.ratingBar.TabIndex = 0;
            this.ratingBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.ratingBar.Value = 5;
            // 
            // CompareForm
            // 
            this.AcceptButton = this.rateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.previewTable);
            this.Controls.Add(this.controlLayoutTable);
            this.Name = "CompareForm";
            this.Text = "CompareForm";
            this.previewTable.ResumeLayout(false);
            this.controlLayoutTable.ResumeLayout(false);
            this.controlLayoutTable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ratingBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel previewTable;
        private System.Windows.Forms.Button rateButton;
        private System.Windows.Forms.Button skipButton;
        private System.Windows.Forms.TableLayoutPanel controlLayoutTable;
        private System.Windows.Forms.TrackBar ratingBar;
    }
}
