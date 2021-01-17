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
            this.addButton = new System.Windows.Forms.Button();
            this.existingPeople = new System.Windows.Forms.FlowLayoutPanel();
            this.personSearchBox = new MediaLibrary.PersonSearchBox();
            this.SuspendLayout();
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
            // personSearchBox
            // 
            this.personSearchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.personSearchBox.Location = new System.Drawing.Point(12, 14);
            this.personSearchBox.Name = "personSearchBox";
            this.personSearchBox.SelectedItem = null;
            this.personSearchBox.Size = new System.Drawing.Size(247, 21);
            this.personSearchBox.TabIndex = 0;
            this.personSearchBox.SelectedItemChanged += new System.EventHandler<System.EventArgs>(this.PersonSearchBox_SelectedPersonChanged);
            this.personSearchBox.TextUpdate += new System.EventHandler(this.PersonSearchBox_TextUpdate);
            this.personSearchBox.TextChanged += new System.EventHandler(this.PersonSearchBox_TextChanged);
            // 
            // AddPeopleForm
            // 
            this.AcceptButton = this.addButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 119);
            this.Controls.Add(this.existingPeople);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.personSearchBox);
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

        private PersonSearchBox personSearchBox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.FlowLayoutPanel existingPeople;
    }
}
