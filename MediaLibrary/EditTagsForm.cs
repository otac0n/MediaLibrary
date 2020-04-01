// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class EditTagsForm : Form
    {
        private readonly MediaIndex index;
        private readonly HashSet<string> rejectedTags = new HashSet<string>();
        private readonly IList<SearchResult> searchResults;
        private readonly Dictionary<string, TagControl> suggestionControls = new Dictionary<string, TagControl>();
        private readonly Dictionary<string, TagControl> tagControls = new Dictionary<string, TagControl>();
        private Dictionary<string, int> tagCounts;

        public EditTagsForm(MediaIndex index, IList<SearchResult> searchResults)
        {
            this.InitializeComponent();
            this.index = index;
            this.searchResults = searchResults;
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

            await this.AddTagAndUpdate(tag).ConfigureAwait(true);
        }

        private void AddOrUpdateSuggestions()
        {
            this.suggestedTags.Controls.Clear();
            var threshold = this.searchResults.Count / 2 + 1;
            var result = this.index.TagEngine.Analyze(this.tagCounts.Where(t => t.Value >= threshold).Select(t => t.Key));
            var missingTags = new HashSet<string>(result.MissingTagSets.Where(t => t.Count == 1).SelectMany(t => t));

            foreach (var tag in result.SuggestedTags)
            {
                if (!this.rejectedTags.Contains(tag))
                {
                    this.AddSuggestionControl(tag, missingTags.Contains(tag));
                }
            }
        }

        private TagControl AddSuggestionControl(string tag, bool isMissing)
        {
            if (!this.suggestionControls.TryGetValue(tag, out var suggestionControl))
            {
                suggestionControl = new TagControl
                {
                    Text = tag,
                    Tag = tag,
                    Indeterminate = !isMissing,
                    AllowDelete = true,
                };
                suggestionControl.MouseClick += this.SuggestionControl_MouseClick;
                suggestionControl.DeleteClick += this.SuggestionControl_DeleteClick;
                suggestionControl.Cursor = Cursors.Hand;
                this.suggestionControls[tag] = suggestionControl;
            }
            else
            {
                suggestionControl.Indeterminate = !isMissing;
            }

            this.suggestedTags.Controls.Add(suggestionControl);
            return suggestionControl;
        }

        private async Task AddTagAndUpdate(string tag)
        {
            if (this.tagControls.TryGetValue(tag, out var tagControl))
            {
                tagControl.Indeterminate = false;
            }
            else
            {
                tagControl = this.AddTagControl(tag, indeterminate: false);
            }

            foreach (var searchResult in this.searchResults)
            {
                await this.index.AddHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(true);
            }

            this.tagCounts[tag] = this.searchResults.Count;
            this.rejectedTags.Remove(tag);
            this.AddOrUpdateSuggestions();
            this.tagLayoutPanel.ScrollControlIntoView(tagControl);
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

        private Dictionary<string, int> CountTags()
        {
            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in this.searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            return tagCounts;
        }

        private async void EditTagsForm_Load(object sender, EventArgs e)
        {
            this.Enabled = false;
            await this.PopulateExistingTags().ConfigureAwait(true);
            this.PopulateTagsCombo();
            this.Enabled = true;
        }

        private async Task PopulateExistingTags()
        {
            this.tagCounts = this.CountTags();
            foreach (var tag in this.tagCounts)
            {
                this.AddTagControl(tag.Key, tag.Value != this.searchResults.Count);
            }

            foreach (var searchResult in this.searchResults)
            {
                this.rejectedTags.UnionWith(
                    (await this.index.GetRejectedTags(searchResult.Hash).ConfigureAwait(true)).Select(t => t.Tag));
            }

            this.AddOrUpdateSuggestions();
        }

        private async void PopulateTagsCombo()
        {
            var tags = await this.index.GetAllTags().ConfigureAwait(true);
            var text = this.tagCombo.Text;
            this.tagCombo.DataSource = tags;
            this.tagCombo.Text = text;
        }

        private void RemoveSuggestionControl(TagControl tagControl, bool destroy = false)
        {
            this.suggestedTags.Controls.Remove(tagControl);
            if (destroy)
            {
                var tag = (string)tagControl.Tag;
                tagControl.MouseClick -= this.SuggestionControl_MouseClick;
                tagControl.DeleteClick -= this.SuggestionControl_DeleteClick;
                this.suggestionControls.Remove(tag);
                tagControl.Dispose();
            }
        }

        private void RemoveTagControl(TagControl tagControl, bool destroy)
        {
            this.existingTags.Controls.Remove(tagControl);
            if (destroy)
            {
                var tag = (string)tagControl.Tag;
                tagControl.DeleteClick -= this.TagControl_DeleteClick;
                this.tagControls.Remove(tag);
                tagControl.Dispose();
            }
        }

        private async void SuggestionControl_DeleteClick(object sender, EventArgs e)
        {
            var tagControl = (TagControl)sender;
            var tag = (string)tagControl.Tag;
            this.RemoveSuggestionControl(tagControl, destroy: true);

            if (this.tagControls.TryGetValue(tag, out tagControl))
            {
                this.RemoveTagControl(tagControl, destroy: true);
            }

            foreach (var searchResult in this.searchResults)
            {
                await this.index.RemoveHashTag(new HashTag(searchResult.Hash, tag), rejectTag: true).ConfigureAwait(true);
            }

            this.tagCounts.Remove(tag);
            this.rejectedTags.Add(tag);
            this.AddOrUpdateSuggestions();
        }

        private async void SuggestionControl_MouseClick(object sender, MouseEventArgs e)
        {
            var tagControl = (TagControl)sender;
            var tag = (string)tagControl.Tag;
            this.RemoveSuggestionControl(tagControl);

            await this.AddTagAndUpdate(tag).ConfigureAwait(true);
        }

        private async void TagControl_DeleteClick(object sender, EventArgs e)
        {
            var tagControl = (TagControl)sender;
            var tag = (string)tagControl.Tag;
            this.RemoveTagControl(tagControl, destroy: true);

            foreach (var searchResult in this.searchResults)
            {
                await this.index.RemoveHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(true);
            }

            this.tagCounts.Remove(tag);
            this.AddOrUpdateSuggestions();
        }
    }
}
