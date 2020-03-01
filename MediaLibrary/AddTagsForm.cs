// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class AddTagsForm : Form
    {
        private readonly MediaIndex index;
        private readonly SearchResult searchResult;

        public AddTagsForm(MediaIndex index, SearchResult searchResult)
        {
            this.InitializeComponent();
            this.index = index;
            this.searchResult = searchResult;
            this.PopulateTagsCombo();
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            var hashTag = new HashTag(this.searchResult.Hash, this.tagCombo.Text.Trim());
            this.tagCombo.Text = string.Empty;
            this.tagCombo.Focus();
            await this.index.AddHashTag(hashTag).ConfigureAwait(false);
        }

        private void AddTagsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private async void PopulateTagsCombo()
        {
            var tags = await this.index.GetAllTags().ConfigureAwait(true);
            var text = this.tagCombo.Text;
            this.tagCombo.DataSource = tags;
            this.tagCombo.Text = text;
        }
    }
}
