namespace MediaLibrary
{
    partial class EditTagRulesForm
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
            this.applyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.rulePages = new System.Windows.Forms.TabControl();
            this.defaultPage = new System.Windows.Forms.TabPage();
            this.rules = new System.Windows.Forms.TextBox();
            this.tabContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.renameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeCategoryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addCategoryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rulePages.SuspendLayout();
            this.defaultPage.SuspendLayout();
            this.tabContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // applyButton
            // 
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.applyButton.Enabled = false;
            this.applyButton.Location = new System.Drawing.Point(276, 515);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 1;
            this.applyButton.Text = "&Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(195, 515);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(357, 515);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // rulePages
            // 
            this.rulePages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rulePages.Controls.Add(this.defaultPage);
            this.rulePages.Enabled = false;
            this.rulePages.Location = new System.Drawing.Point(12, 12);
            this.rulePages.Name = "rulePages";
            this.rulePages.SelectedIndex = 0;
            this.rulePages.Size = new System.Drawing.Size(420, 497);
            this.rulePages.TabIndex = 4;
            this.rulePages.DragDrop += this.RulePages_DragDrop;
            this.rulePages.DragOver += this.RulePages_DragOver;
            this.rulePages.MouseClick += this.RulePages_MouseClick;
            this.rulePages.MouseDown += this.RulePages_MouseDown;
            this.rulePages.MouseUp += this.RulePages_MouseUp;
            // 
            // defaultPage
            // 
            this.defaultPage.Controls.Add(this.rules);
            this.defaultPage.Location = new System.Drawing.Point(4, 22);
            this.defaultPage.Name = "defaultPage";
            this.defaultPage.Padding = new System.Windows.Forms.Padding(3);
            this.defaultPage.Size = new System.Drawing.Size(412, 471);
            this.defaultPage.TabIndex = 0;
            this.defaultPage.Text = "(Default)";
            this.defaultPage.UseVisualStyleBackColor = true;
            // 
            // rules
            // 
            this.rules.AcceptsReturn = true;
            this.rules.AcceptsTab = true;
            this.rules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rules.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rules.HideSelection = false;
            this.rules.Location = new System.Drawing.Point(0, 0);
            this.rules.MaxLength = 0;
            this.rules.Multiline = true;
            this.rules.Name = "rules";
            this.rules.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.rules.Size = new System.Drawing.Size(412, 471);
            this.rules.TabIndex = 1;
            this.rules.WordWrap = false;
            // 
            // tabContextMenu
            // 
            this.tabContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.renameMenuItem,
            this.removeCategoryMenuItem,
            this.addCategoryMenuItem});
            this.tabContextMenu.Name = "tabContextMenu";
            this.tabContextMenu.Size = new System.Drawing.Size(169, 70);
            // 
            // renameMenuItem
            // 
            this.renameMenuItem.Image = global::MediaLibrary.Properties.Resources.tags_edit;
            this.renameMenuItem.Name = "renameMenuItem";
            this.renameMenuItem.Size = new System.Drawing.Size(168, 22);
            this.renameMenuItem.Text = "Re&name...";
            this.renameMenuItem.Click += new System.EventHandler(this.RenameMenuItem_Click);
            // 
            // removeCategoryMenuItem
            // 
            this.removeCategoryMenuItem.Image = global::MediaLibrary.Properties.Resources.tags_remove;
            this.removeCategoryMenuItem.Name = "removeCategoryMenuItem";
            this.removeCategoryMenuItem.Size = new System.Drawing.Size(168, 22);
            this.removeCategoryMenuItem.Text = "&Remove Category";
            this.removeCategoryMenuItem.Click += new System.EventHandler(this.RemoveCategoryMenuItem_Click);
            // 
            // addCategoryMenuItem
            // 
            this.addCategoryMenuItem.Image = global::MediaLibrary.Properties.Resources.tags_add;
            this.addCategoryMenuItem.Name = "addCategoryMenuItem";
            this.addCategoryMenuItem.Size = new System.Drawing.Size(168, 22);
            this.addCategoryMenuItem.Text = "&Add Category...";
            this.addCategoryMenuItem.Click += new System.EventHandler(this.AddCategoryMenuItem_Click);
            // 
            // EditTagRulesForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(444, 550);
            this.Controls.Add(this.rulePages);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.cancelButton);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditTagRulesForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Tag Rules";
            this.Load += new System.EventHandler(this.EditTagRulesForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditTagRulesForm_KeyDown);
            this.rulePages.ResumeLayout(false);
            this.defaultPage.ResumeLayout(false);
            this.defaultPage.PerformLayout();
            this.tabContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.TabControl rulePages;
        private System.Windows.Forms.TabPage defaultPage;
        private System.Windows.Forms.TextBox rules;
        private System.Windows.Forms.ContextMenuStrip tabContextMenu;
        private System.Windows.Forms.ToolStripMenuItem removeCategoryMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addCategoryMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameMenuItem;
    }
}
