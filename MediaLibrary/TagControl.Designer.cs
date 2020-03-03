namespace MediaLibrary
{
    partial class TagControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tagName = new System.Windows.Forms.Label();
            this.deleteButton = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.deleteButton)).BeginInit();
            this.SuspendLayout();
            // 
            // tagName
            // 
            this.tagName.AutoSize = true;
            this.tagName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tagName.Location = new System.Drawing.Point(3, 3);
            this.tagName.Margin = new System.Windows.Forms.Padding(3, 0, 23, 0);
            this.tagName.Name = "tagName";
            this.tagName.Size = new System.Drawing.Size(28, 13);
            this.tagName.TabIndex = 0;
            this.tagName.Text = "tag1";
            // 
            // deleteButton
            // 
            this.deleteButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.deleteButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.deleteButton.Image = global::MediaLibrary.Properties.Resources.remove_circle;
            this.deleteButton.Location = new System.Drawing.Point(31, 3);
            this.deleteButton.Margin = new System.Windows.Forms.Padding(0);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(20, 13);
            this.deleteButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.deleteButton.TabIndex = 2;
            this.deleteButton.TabStop = false;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // TagControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.SystemColors.Info;
            this.Controls.Add(this.tagName);
            this.Controls.Add(this.deleteButton);
            this.Name = "TagControl";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.Size = new System.Drawing.Size(54, 19);
            ((System.ComponentModel.ISupportInitialize)(this.deleteButton)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label tagName;
        private System.Windows.Forms.PictureBox deleteButton;
    }
}
