// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class EditTagsForm : Form
    {
        private readonly MediaIndex index;
        private readonly IList<SearchResult> searchResults;

        public EditTagsForm(MediaIndex index, IList<SearchResult> searchResults)
        {
            this.InitializeComponent();
            this.index = index;
            this.searchResults = searchResults;
            this.PopulateExistingTags();
            this.PopulateTagsCombo();
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            var tag = this.tagCombo.Text.Trim();
            this.tagCombo.Text = string.Empty;
            this.tagCombo.Focus();

            if (this.existingTags.Controls.ContainsKey(tag))
            {
                var tagControl = (TagControl)this.existingTags.Controls[tag];
                // TODO: Make determinate.
            }
            else
            {
                this.AddTagControl(tag);
            }

            foreach (var searchResult in this.searchResults)
            {
                await this.index.AddHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(false);
            }
        }

        private void AddTagControl(string tag)
        {
            // TODO: Indeterminate.
            var tagControl = new TagControl { Text = tag, Tag = tag };
            tagControl.DeleteClick += this.TagControl_DeleteClick;
            this.existingTags.Controls.Add(tagControl);
        }

        private void AddTagsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void PopulateExistingTags()
        {
            foreach (var tag in this.searchResults.SelectMany(r => r.Tags).Distinct())
            {
                this.AddTagControl(tag);
            }
        }

        private async void PopulateTagsCombo()
        {
            var tags = await this.index.GetAllTags().ConfigureAwait(true);
            var text = this.tagCombo.Text;
            this.tagCombo.DataSource = tags;
            this.tagCombo.Text = text;
        }

        private async void TagControl_DeleteClick(object sender, EventArgs e)
        {
            var tagControl = (TagControl)sender;
            tagControl.DeleteClick -= this.TagControl_DeleteClick;
            this.existingTags.Controls.Remove(tagControl);

            foreach (var searchResult in this.searchResults)
            {
                var tag = (string)tagControl.Tag;
                await this.index.RemoveHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(false);
            }
        }
    }
}
