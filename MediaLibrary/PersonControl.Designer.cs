namespace MediaLibrary
{
    partial class PersonControl
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
            this.personName = new System.Windows.Forms.Label();
            this.deleteButton = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.deleteButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // personName
            // 
            this.personName.AutoSize = true;
            this.personName.BackColor = System.Drawing.SystemColors.Info;
            this.personName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.personName.ForeColor = System.Drawing.SystemColors.InfoText;
            this.personName.Location = new System.Drawing.Point(20, 0);
            this.personName.Margin = new System.Windows.Forms.Padding(3, 0, 23, 0);
            this.personName.Name = "personName";
            this.personName.Size = new System.Drawing.Size(13, 13);
            this.personName.TabIndex = 1;
            this.personName.Text = "?";
            // 
            // deleteButton
            // 
            this.deleteButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.deleteButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.deleteButton.Image = global::MediaLibrary.Properties.Resources.remove_circle;
            this.deleteButton.Location = new System.Drawing.Point(33, 0);
            this.deleteButton.Margin = new System.Windows.Forms.Padding(0);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(20, 13);
            this.deleteButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.deleteButton.TabIndex = 2;
            this.deleteButton.TabStop = false;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.pictureBox1.Image = global::MediaLibrary.Properties.Resources.single_neutral;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(20, 13);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // PersonControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.personName);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.pictureBox1);
            this.Name = "PersonControl";
            this.Size = new System.Drawing.Size(53, 13);
            ((System.ComponentModel.ISupportInitialize)(this.deleteButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label personName;
        private System.Windows.Forms.PictureBox deleteButton;
    }
}
