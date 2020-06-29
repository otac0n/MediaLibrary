namespace MediaLibrary
{
    partial class EditTagsForm
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
            this.components = new System.ComponentModel.Container();
            this.tagCombo = new System.Windows.Forms.ComboBox();
            this.addButton = new System.Windows.Forms.Button();
            this.tagLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.suggestedTags = new System.Windows.Forms.FlowLayoutPanel();
            this.existingTags = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tagLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tagCombo
            // 
            this.tagCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tagCombo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.tagCombo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.tagCombo.FormattingEnabled = true;
            this.tagCombo.Location = new System.Drawing.Point(12, 14);
            this.tagCombo.Name = "tagCombo";
            this.tagCombo.Size = new System.Drawing.Size(279, 21);
            this.tagCombo.TabIndex = 0;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Location = new System.Drawing.Point(297, 12);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 1;
            this.addButton.Text = "&Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // tagLayoutPanel
            // 
            this.tagLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tagLayoutPanel.AutoScroll = true;
            this.tagLayoutPanel.Controls.Add(this.suggestedTags);
            this.tagLayoutPanel.Controls.Add(this.existingTags);
            this.tagLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.tagLayoutPanel.Location = new System.Drawing.Point(12, 45);
            this.tagLayoutPanel.Name = "tagLayoutPanel";
            this.tagLayoutPanel.Size = new System.Drawing.Size(360, 204);
            this.tagLayoutPanel.TabIndex = 4;
            this.tagLayoutPanel.WrapContents = false;
            // 
            // suggestedTags
            // 
            this.suggestedTags.AutoSize = true;
            this.suggestedTags.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.suggestedTags.Location = new System.Drawing.Point(3, 3);
            this.suggestedTags.Name = "suggestedTags";
            this.suggestedTags.Size = new System.Drawing.Size(0, 0);
            this.suggestedTags.TabIndex = 4;
            // 
            // existingTags
            // 
            this.existingTags.AutoSize = true;
            this.existingTags.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.existingTags.Location = new System.Drawing.Point(3, 9);
            this.existingTags.Name = "existingTags";
            this.existingTags.Size = new System.Drawing.Size(0, 0);
            this.existingTags.TabIndex = 5;
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 0;
            // 
            // EditTagsForm
            // 
            this.AcceptButton = this.addButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.tagLayoutPanel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.tagCombo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 200);
            this.Name = "EditTagsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Tags";
            this.Load += new System.EventHandler(this.EditTagsForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AddTagsForm_KeyDown);
            this.tagLayoutPanel.ResumeLayout(false);
            this.tagLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox tagCombo;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.FlowLayoutPanel tagLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel suggestedTags;
        private System.Windows.Forms.FlowLayoutPanel existingTags;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
