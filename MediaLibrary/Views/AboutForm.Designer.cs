namespace MediaLibrary.Views
{
    partial class AboutForm
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
            this.iconAttributionLink = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            this.titleLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // iconAttributionLink
            // 
            this.iconAttributionLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.iconAttributionLink.Image = Properties.Resources.mood_happy;
            this.iconAttributionLink.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.iconAttributionLink.Location = new System.Drawing.Point(235, 194);
            this.iconAttributionLink.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.iconAttributionLink.Name = "iconAttributionLink";
            this.iconAttributionLink.Size = new System.Drawing.Size(395, 44);
            this.iconAttributionLink.TabIndex = 1;
            this.iconAttributionLink.TabStop = true;
            this.iconAttributionLink.Text = "Free Icons from the Streamline Icons Pack";
            this.iconAttributionLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.iconAttributionLink.LinkClicked += this.IconAttributionLink_LinkClicked;
            // 
            // closeButton
            // 
            this.closeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(505, 265);
            this.closeButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(125, 44);
            this.closeButton.TabIndex = 0;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new System.Drawing.Point(22, 25);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(114, 25);
            this.titleLabel.TabIndex = 2;
            this.titleLabel.Text = "MediaLibrary";
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(22, 50);
            this.versionLabel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(36, 25);
            this.versionLabel.TabIndex = 3;
            this.versionLabel.Text = "1.0";
            // 
            // AboutForm
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(650, 333);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.iconAttributionLink);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.LinkLabel iconAttributionLink;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
    }
}