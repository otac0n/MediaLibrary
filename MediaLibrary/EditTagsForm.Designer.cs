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
            this.tagSearchBox = new MediaLibrary.TagSearchBox();
            this.addButton = new System.Windows.Forms.Button();
            this.tagLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.suggestedTags = new System.Windows.Forms.FlowLayoutPanel();
            this.existingTags = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tagContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeTagMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rejectTagMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.suggestionContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.rejectSuggestionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tagLayoutPanel.SuspendLayout();
            this.tagContextMenu.SuspendLayout();
            this.suggestionContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tagSearchBox
            // 
            this.tagSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tagSearchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.tagSearchBox.FormattingEnabled = true;
            this.tagSearchBox.Location = new System.Drawing.Point(12, 14);
            this.tagSearchBox.Name = "tagSearchBox";
            this.tagSearchBox.SelectedItem = null;
            this.tagSearchBox.Size = new System.Drawing.Size(279, 21);
            this.tagSearchBox.TabIndex = 0;
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
            // tagContextMenu
            // 
            this.tagContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeTagMenuItem,
            this.rejectTagMenuItem});
            this.tagContextMenu.Name = "tagContextMenu";
            this.tagContextMenu.Size = new System.Drawing.Size(139, 48);
            // 
            // removeTagMenuItem
            // 
            this.removeTagMenuItem.Name = "removeTagMenuItem";
            this.removeTagMenuItem.Size = new System.Drawing.Size(138, 22);
            this.removeTagMenuItem.Text = "Remove Tag";
            this.removeTagMenuItem.Click += new System.EventHandler(this.RemoveTagMenuItem_Click);
            // 
            // rejectTagMenuItem
            // 
            this.rejectTagMenuItem.Name = "rejectTagMenuItem";
            this.rejectTagMenuItem.Size = new System.Drawing.Size(138, 22);
            this.rejectTagMenuItem.Text = "Reject Tag";
            this.rejectTagMenuItem.Click += new System.EventHandler(this.RejectTagMenuItem_Click);
            // 
            // suggestionContextMenu
            // 
            this.suggestionContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rejectSuggestionMenuItem});
            this.suggestionContextMenu.Name = "suggestionContextMenu";
            this.suggestionContextMenu.Size = new System.Drawing.Size(169, 26);
            // 
            // rejectSuggestionMenuItem
            // 
            this.rejectSuggestionMenuItem.Name = "rejectSuggestionMenuItem";
            this.rejectSuggestionMenuItem.Size = new System.Drawing.Size(168, 22);
            this.rejectSuggestionMenuItem.Text = "Reject Suggestion";
            this.rejectSuggestionMenuItem.Click += new System.EventHandler(this.RejectSuggestionMenuItem_Click);
            // 
            // EditTagsForm
            // 
            this.AcceptButton = this.addButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.tagLayoutPanel);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.tagSearchBox);
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
            this.tagContextMenu.ResumeLayout(false);
            this.suggestionContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TagSearchBox tagSearchBox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.FlowLayoutPanel tagLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel suggestedTags;
        private System.Windows.Forms.FlowLayoutPanel existingTags;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ContextMenuStrip tagContextMenu;
        private System.Windows.Forms.ToolStripMenuItem removeTagMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rejectTagMenuItem;
        private System.Windows.Forms.ContextMenuStrip suggestionContextMenu;
        private System.Windows.Forms.ToolStripMenuItem rejectSuggestionMenuItem;
    }
}
