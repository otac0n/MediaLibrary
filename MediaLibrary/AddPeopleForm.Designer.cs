namespace MediaLibrary
{
    partial class AddPeopleForm
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
            this.personCombo = new System.Windows.Forms.ComboBox();
            this.addButton = new System.Windows.Forms.Button();
            this.existingPeople = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // personCombo
            // 
            this.personCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.personCombo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.personCombo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.personCombo.FormattingEnabled = true;
            this.personCombo.Location = new System.Drawing.Point(12, 14);
            this.personCombo.Name = "personCombo";
            this.personCombo.Size = new System.Drawing.Size(247, 21);
            this.personCombo.TabIndex = 0;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point(265, 12);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 1;
            this.addButton.Text = "&Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // existingPeople
            // 
            this.existingPeople.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.existingPeople.AutoScroll = true;
            this.existingPeople.Location = new System.Drawing.Point(12, 41);
            this.existingPeople.Name = "existingPeople";
            this.existingPeople.Size = new System.Drawing.Size(328, 66);
            this.existingPeople.TabIndex = 2;
            // 
            // AddPeopleForm
            // 
            this.AcceptButton = this.addButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 119);
            this.Controls.Add(this.existingPeople);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.personCombo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddPeopleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add People";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AddPeopleForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox personCombo;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.FlowLayoutPanel existingPeople;
    }
}
