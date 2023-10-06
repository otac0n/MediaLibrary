// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Windows.Forms;

    public partial class TextSearchForm : Form
    {
        public string Search
        {
            get => this.SearchText.Text;
            set => this.SearchText.Text = value;
        }

        public EventHandler FindNext;
        public EventHandler FindPrevious;

        public TextSearchForm()
        {
            this.InitializeComponent();
        }

        private void TextSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        public void Refocus()
        {
            this.SearchText.SelectAll();
            this.SearchText.Focus();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Hide();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        private void FindNextButton_Click(object sender, EventArgs e)
        {
            this.FindNext?.Invoke(this, e);
        }

        private void FindPreviousButton_Click(object sender, EventArgs e)
        {
            this.FindPrevious?.Invoke(this, e);
        }
    }
}
