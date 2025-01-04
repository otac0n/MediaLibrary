// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Views
{
    using System;
    using System.IO;
    using System.Media;
    using System.Security;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class AddIndexedPathForm : Form
    {
        private readonly MediaIndex index;

        public AddIndexedPathForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        public string SelectedPath
        {
            get
            {
                var text = this.path.Text;
                if (string.IsNullOrEmpty(text))
                {
                    return text;
                }

                try
                {
                    return Path.GetFullPath(text);
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException || ex is SecurityException || ex is NotSupportedException || ex is PathTooLongException)
                    {
                        return text;
                    }

                    throw;
                }
            }
        }

        public string Include
        {
            get
            {
                var text = this.include.Text;
                return string.IsNullOrEmpty(text) ? null : text;
            }

            set => this.include.Text = value ?? string.Empty;
        }

        public string Exclude
        {
            get
            {
                var text = this.exclude.Text;
                return string.IsNullOrEmpty(text) ? null : text;
            }

            set => this.exclude.Text = value ?? string.Empty;
        }

        private void BrowseButton_Click(object sender, System.EventArgs e)
        {
            this.folderBrowserDialog.SelectedPath = this.path.Text;
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.path.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private void FinishButton_Click(object sender, System.EventArgs e)
        {
            if (Directory.Exists(this.SelectedPath))
            {
                this.DialogResult = DialogResult.OK;
                this.Hide();
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
    }
}
