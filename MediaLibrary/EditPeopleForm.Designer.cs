namespace MediaLibrary
{
    partial class EditPeopleForm
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
            this.addNewPersonMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editorTablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.aliasesTablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.usernamesLabel = new System.Windows.Forms.Label();
            this.addUsernameFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.usernamesFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.addUsernameButton = new System.Windows.Forms.Button();
            this.usernameTextBox = new System.Windows.Forms.TextBox();
            this.siteTextBox = new System.Windows.Forms.TextBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.advancedButton = new System.Windows.Forms.Button();
            this.advancedMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deletePersonMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.personSearchBox = new MediaLibrary.PersonSearchBox();
            this.editorTablePanel.SuspendLayout();
            this.addUsernameFlowPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.advancedMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // addNewPersonMenuItem
            // 
            this.addNewPersonMenuItem.Enabled = false;
            this.addNewPersonMenuItem.Image = global::MediaLibrary.Properties.Resources.add_circle;
            this.addNewPersonMenuItem.Name = "addNewPersonMenuItem";
            this.addNewPersonMenuItem.Size = new System.Drawing.Size(169, 22);
            this.addNewPersonMenuItem.Text = "Add new person...";
            this.addNewPersonMenuItem.Click += new System.EventHandler(this.AddNewPersonMenuItem_Click);
            // 
            // editorTablePanel
            // 
            this.editorTablePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editorTablePanel.AutoScroll = true;
            this.editorTablePanel.ColumnCount = 2;
            this.editorTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.editorTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.editorTablePanel.Controls.Add(this.nameLabel, 0, 0);
            this.editorTablePanel.Controls.Add(this.nameTextBox, 1, 0);
            this.editorTablePanel.Controls.Add(this.aliasesTablePanel, 1, 1);
            this.editorTablePanel.Controls.Add(this.usernamesLabel, 0, 2);
            this.editorTablePanel.Controls.Add(this.addUsernameFlowPanel, 1, 2);
            this.editorTablePanel.Enabled = false;
            this.editorTablePanel.Location = new System.Drawing.Point(12, 41);
            this.editorTablePanel.Name = "editorTablePanel";
            this.editorTablePanel.RowCount = 3;
            this.editorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.editorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.editorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.editorTablePanel.Size = new System.Drawing.Size(546, 296);
            this.editorTablePanel.TabIndex = 1;
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nameLabel.Location = new System.Drawing.Point(3, 5);
            this.nameLabel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(77, 26);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "&Name:";
            // 
            // nameTextBox
            // 
            this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nameTextBox.Location = new System.Drawing.Point(93, 3);
            this.nameTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 3, 3);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(450, 32);
            this.nameTextBox.TabIndex = 1;
            this.nameTextBox.Validated += new System.EventHandler(this.NameTextBox_Validated);
            // 
            // aliasesTablePanel
            // 
            this.aliasesTablePanel.AutoSize = true;
            this.aliasesTablePanel.ColumnCount = 2;
            this.aliasesTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.aliasesTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.aliasesTablePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.aliasesTablePanel.Location = new System.Drawing.Point(86, 41);
            this.aliasesTablePanel.Name = "aliasesTablePanel";
            this.aliasesTablePanel.RowCount = 1;
            this.aliasesTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.aliasesTablePanel.Size = new System.Drawing.Size(457, 0);
            this.aliasesTablePanel.TabIndex = 3;
            // 
            // usernamesLabel
            // 
            this.usernamesLabel.AutoSize = true;
            this.usernamesLabel.Location = new System.Drawing.Point(3, 44);
            this.usernamesLabel.Name = "usernamesLabel";
            this.usernamesLabel.Size = new System.Drawing.Size(63, 13);
            this.usernamesLabel.TabIndex = 2;
            this.usernamesLabel.Text = "&Usernames:";
            // 
            // addUsernameFlowPanel
            // 
            this.addUsernameFlowPanel.AutoSize = true;
            this.addUsernameFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.addUsernameFlowPanel.Controls.Add(this.usernamesFlowPanel);
            this.addUsernameFlowPanel.Controls.Add(this.panel1);
            this.addUsernameFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.addUsernameFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.addUsernameFlowPanel.Location = new System.Drawing.Point(86, 47);
            this.addUsernameFlowPanel.Name = "addUsernameFlowPanel";
            this.addUsernameFlowPanel.Size = new System.Drawing.Size(457, 246);
            this.addUsernameFlowPanel.TabIndex = 5;
            // 
            // usernamesFlowPanel
            // 
            this.usernamesFlowPanel.AutoSize = true;
            this.usernamesFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.usernamesFlowPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.usernamesFlowPanel.Location = new System.Drawing.Point(3, 3);
            this.usernamesFlowPanel.Name = "usernamesFlowPanel";
            this.usernamesFlowPanel.Size = new System.Drawing.Size(324, 0);
            this.usernamesFlowPanel.TabIndex = 6;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.addUsernameButton);
            this.panel1.Controls.Add(this.usernameTextBox);
            this.panel1.Controls.Add(this.siteTextBox);
            this.panel1.Location = new System.Drawing.Point(3, 9);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(324, 30);
            this.panel1.TabIndex = 0;
            // 
            // addUsernameButton
            // 
            this.addUsernameButton.Location = new System.Drawing.Point(246, 3);
            this.addUsernameButton.Name = "addUsernameButton";
            this.addUsernameButton.Size = new System.Drawing.Size(75, 23);
            this.addUsernameButton.TabIndex = 2;
            this.addUsernameButton.Text = "Add Username";
            this.addUsernameButton.UseVisualStyleBackColor = true;
            this.addUsernameButton.Click += new System.EventHandler(this.AddUsernameButton_Click);
            // 
            // usernameTextBox
            // 
            this.usernameTextBox.Location = new System.Drawing.Point(110, 5);
            this.usernameTextBox.Name = "usernameTextBox";
            this.usernameTextBox.Size = new System.Drawing.Size(130, 20);
            this.usernameTextBox.TabIndex = 1;
            // 
            // siteTextBox
            // 
            this.siteTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.siteTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.siteTextBox.Location = new System.Drawing.Point(4, 5);
            this.siteTextBox.Name = "siteTextBox";
            this.siteTextBox.Size = new System.Drawing.Size(100, 20);
            this.siteTextBox.TabIndex = 0;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // advancedButton
            // 
            this.advancedButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.advancedButton.Location = new System.Drawing.Point(529, 13);
            this.advancedButton.Name = "advancedButton";
            this.advancedButton.Size = new System.Drawing.Size(29, 21);
            this.advancedButton.TabIndex = 2;
            this.advancedButton.Text = "...";
            this.advancedButton.UseVisualStyleBackColor = true;
            this.advancedButton.Click += new System.EventHandler(this.AdvancedButton_Click);
            // 
            // advancedMenuStrip
            // 
            this.advancedMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewPersonMenuItem,
            this.deletePersonMenuItem});
            this.advancedMenuStrip.Name = "advancedMenuStrip";
            this.advancedMenuStrip.Size = new System.Drawing.Size(170, 48);
            // 
            // deletePersonMenuItem
            // 
            this.deletePersonMenuItem.Enabled = false;
            this.deletePersonMenuItem.Image = global::MediaLibrary.Properties.Resources.remove_circle_red;
            this.deletePersonMenuItem.Name = "deletePersonMenuItem";
            this.deletePersonMenuItem.Size = new System.Drawing.Size(169, 22);
            this.deletePersonMenuItem.Text = "Delete person...";
            this.deletePersonMenuItem.Click += new System.EventHandler(this.DeletePersonMenuItem_Click);
            // 
            // personSearchBox
            // 
            this.personSearchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.personSearchBox.Location = new System.Drawing.Point(105, 13);
            this.personSearchBox.Name = "personSearchBox";
            this.personSearchBox.SelectedItem = null;
            this.personSearchBox.Size = new System.Drawing.Size(418, 21);
            this.personSearchBox.TabIndex = 0;
            this.personSearchBox.SelectedItemChanged += new System.EventHandler<System.EventArgs>(this.PersonSearchBox_SelectedPersonChanged);
            this.personSearchBox.TextUpdate += new System.EventHandler(this.PersonSearchBox_TextUpdate);
            // 
            // EditPeopleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(570, 349);
            this.Controls.Add(this.advancedButton);
            this.Controls.Add(this.editorTablePanel);
            this.Controls.Add(this.personSearchBox);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditPeopleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit People";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditPersonForm_KeyDown);
            this.editorTablePanel.ResumeLayout(false);
            this.editorTablePanel.PerformLayout();
            this.addUsernameFlowPanel.ResumeLayout(false);
            this.addUsernameFlowPanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.advancedMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private PersonSearchBox personSearchBox;
        private System.Windows.Forms.TableLayoutPanel editorTablePanel;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.TableLayoutPanel aliasesTablePanel;
        private System.Windows.Forms.Label usernamesLabel;
        private System.Windows.Forms.FlowLayoutPanel addUsernameFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel usernamesFlowPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button addUsernameButton;
        private System.Windows.Forms.TextBox usernameTextBox;
        private System.Windows.Forms.TextBox siteTextBox;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Button advancedButton;
        private System.Windows.Forms.ContextMenuStrip advancedMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem deletePersonMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addNewPersonMenuItem;
    }
}
