namespace MediaLibrary
{
    partial class MergePeopleForm
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
            this.personASearchBox = new MediaLibrary.PersonSearchBox();
            this.personALabel = new System.Windows.Forms.Label();
            this.personBLabel = new System.Windows.Forms.Label();
            this.personBSearchBox = new MediaLibrary.PersonSearchBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.instructionsLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // personASearchBox
            // 
            this.personASearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.personASearchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.personASearchBox.Location = new System.Drawing.Point(108, 83);
            this.personASearchBox.Name = "personASearchBox";
            this.personASearchBox.SelectedPerson = null;
            this.personASearchBox.Size = new System.Drawing.Size(382, 21);
            this.personASearchBox.TabIndex = 0;
            this.personASearchBox.SelectedPersonChanged += new System.EventHandler<System.EventArgs>(this.PersonSearchBox_SelectedPersonChanged);
            // 
            // personALabel
            // 
            this.personALabel.AutoSize = true;
            this.personALabel.Location = new System.Drawing.Point(15, 86);
            this.personALabel.Name = "personALabel";
            this.personALabel.Size = new System.Drawing.Size(50, 13);
            this.personALabel.TabIndex = 1;
            this.personALabel.Text = "Person &A";
            // 
            // personBLabel
            // 
            this.personBLabel.AutoSize = true;
            this.personBLabel.Location = new System.Drawing.Point(15, 113);
            this.personBLabel.Name = "personBLabel";
            this.personBLabel.Size = new System.Drawing.Size(50, 13);
            this.personBLabel.TabIndex = 3;
            this.personBLabel.Text = "Person &B";
            // 
            // personBSearchBox
            // 
            this.personBSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.personBSearchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.personBSearchBox.Location = new System.Drawing.Point(108, 110);
            this.personBSearchBox.Name = "personBSearchBox";
            this.personBSearchBox.SelectedPerson = null;
            this.personBSearchBox.Size = new System.Drawing.Size(382, 21);
            this.personBSearchBox.TabIndex = 2;
            this.personBSearchBox.SelectedPersonChanged += new System.EventHandler<System.EventArgs>(this.PersonSearchBox_SelectedPersonChanged);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(147, 26);
            this.titleLabel.TabIndex = 7;
            this.titleLabel.Text = "Merge People";
            // 
            // instructionsLabel
            // 
            this.instructionsLabel.AutoSize = true;
            this.instructionsLabel.Location = new System.Drawing.Point(12, 43);
            this.instructionsLabel.Name = "instructionsLabel";
            this.instructionsLabel.Size = new System.Drawing.Size(257, 13);
            this.instructionsLabel.TabIndex = 6;
            this.instructionsLabel.Text = "Search for the people you would like to merge below.";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(415, 158);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 9;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(322, 158);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // MergePeopleForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(502, 193);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.instructionsLabel);
            this.Controls.Add(this.personBLabel);
            this.Controls.Add(this.personBSearchBox);
            this.Controls.Add(this.personALabel);
            this.Controls.Add(this.personASearchBox);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MergePeopleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit People";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditPersonForm_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PersonSearchBox personASearchBox;
        private System.Windows.Forms.Label personALabel;
        private System.Windows.Forms.Label personBLabel;
        private PersonSearchBox personBSearchBox;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label instructionsLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
