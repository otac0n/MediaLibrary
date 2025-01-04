namespace MediaLibrary.Views
{
    partial class AddIndexedPathForm
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
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.finishButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.browseButton = new System.Windows.Forms.Button();
            this.path = new System.Windows.Forms.TextBox();
            this.folderLabel = new System.Windows.Forms.Label();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.include = new System.Windows.Forms.TextBox();
            this.exclude = new System.Windows.Forms.TextBox();
            this.includeLabel = new System.Windows.Forms.Label();
            this.excludeLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)this.errorProvider).BeginInit();
            this.SuspendLayout();
            // 
            // finishButton
            // 
            this.finishButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.finishButton.Location = new System.Drawing.Point(783, 210);
            this.finishButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.finishButton.Name = "finishButton";
            this.finishButton.Size = new System.Drawing.Size(125, 44);
            this.finishButton.TabIndex = 7;
            this.finishButton.Text = "&Finish";
            this.finishButton.UseVisualStyleBackColor = true;
            this.finishButton.Click += this.FinishButton_Click;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(935, 210);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(125, 44);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += this.CancelButton_Click;
            // 
            // browseButton
            // 
            this.browseButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.browseButton.Location = new System.Drawing.Point(935, 23);
            this.browseButton.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(125, 44);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "&Browse...";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += this.BrowseButton_Click;
            // 
            // path
            // 
            this.path.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.path.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.path.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.path.Location = new System.Drawing.Point(90, 27);
            this.path.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.path.Name = "path";
            this.path.Size = new System.Drawing.Size(832, 31);
            this.path.TabIndex = 1;
            // 
            // folderLabel
            // 
            this.folderLabel.AutoSize = true;
            this.folderLabel.Location = new System.Drawing.Point(20, 33);
            this.folderLabel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.folderLabel.Name = "folderLabel";
            this.folderLabel.Size = new System.Drawing.Size(62, 25);
            this.folderLabel.TabIndex = 0;
            this.folderLabel.Text = "&Folder";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // include
            // 
            this.include.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.include.Location = new System.Drawing.Point(90, 90);
            this.include.Name = "include";
            this.include.PlaceholderText = "*";
            this.include.Size = new System.Drawing.Size(832, 31);
            this.include.TabIndex = 4;
            // 
            // exclude
            // 
            this.exclude.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.exclude.Location = new System.Drawing.Point(90, 127);
            this.exclude.Name = "exclude";
            this.exclude.Size = new System.Drawing.Size(832, 31);
            this.exclude.TabIndex = 6;
            // 
            // includeLabel
            // 
            this.includeLabel.AutoSize = true;
            this.includeLabel.Location = new System.Drawing.Point(20, 93);
            this.includeLabel.Name = "includeLabel";
            this.includeLabel.Size = new System.Drawing.Size(69, 25);
            this.includeLabel.TabIndex = 3;
            this.includeLabel.Text = "I&nclude";
            // 
            // excludeLabel
            // 
            this.excludeLabel.AutoSize = true;
            this.excludeLabel.Location = new System.Drawing.Point(20, 130);
            this.excludeLabel.Name = "excludeLabel";
            this.excludeLabel.Size = new System.Drawing.Size(71, 25);
            this.excludeLabel.TabIndex = 5;
            this.excludeLabel.Text = "E&xclude";
            // 
            // AddIndexedPathForm
            // 
            this.AcceptButton = this.finishButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(1080, 277);
            this.Controls.Add(this.excludeLabel);
            this.Controls.Add(this.includeLabel);
            this.Controls.Add(this.exclude);
            this.Controls.Add(this.include);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.path);
            this.Controls.Add(this.folderLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.finishButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddIndexedPathForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Indexed Folder";
            ((System.ComponentModel.ISupportInitialize)this.errorProvider).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button finishButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox path;
        private System.Windows.Forms.Label folderLabel;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Label excludeLabel;
        private System.Windows.Forms.Label includeLabel;
        private System.Windows.Forms.TextBox exclude;
        private System.Windows.Forms.TextBox include;
    }
}
