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
        private readonly Dictionary<string, TagControl> tagControls = new Dictionary<string, TagControl>();

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

            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (this.tagControls.TryGetValue(tag, out var tagControl))
            {
                tagControl.Indeterminate = false;
                this.existingTags.ScrollControlIntoView(tagControl);
            }
            else
            {
                tagControl = this.AddTagControl(tag, indeterminate: false);
                this.existingTags.ScrollControlIntoView(tagControl);
            }

            foreach (var searchResult in this.searchResults)
            {
                await this.index.AddHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(false);
            }
        }

        private TagControl AddTagControl(string tag, bool indeterminate)
        {
            var tagControl = new TagControl { Text = tag, Tag = tag, Indeterminate = indeterminate };
            tagControl.DeleteClick += this.TagControl_DeleteClick;
            this.existingTags.Controls.Add(this.tagControls[tag] = tagControl);
            return tagControl;
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
            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in this.searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            foreach (var tag in tagCounts)
            {
                this.AddTagControl(tag.Key, tag.Value != this.searchResults.Count);
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
