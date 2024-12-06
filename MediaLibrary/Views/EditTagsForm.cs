// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Views
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Components;
    using MediaLibrary.Services;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class EditTagsForm : Form
    {
        private readonly MediaIndex index;
        private readonly IList<SearchResult> searchResults;
        private readonly List<string> tagsInEntryOrder = new List<string>();
        private readonly List<string> rejectedInEntryOrder = new List<string>();
        private Dictionary<string, int> tagCounts;
        private Dictionary<string, int> rejectedCounts;

        public EditTagsForm(MediaIndex index, IList<SearchResult> searchResults)
        {
            this.InitializeComponent();
            this.advancedButton.AttachDropDownMenu(this.advancedMenuStrip, this.components);
            this.index = index;
            this.searchResults = searchResults;
        }

        private string AcceptTagInput()
        {
            var tag = this.tagSearchBox.Text.Trim();
            this.tagSearchBox.Text = string.Empty;
            this.tagSearchBox.Focus();
            return tag;
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            await this.AddSelectedTag().ConfigureAwait(true);
        }

        private async Task AddSelectedTag()
        {
            var tag = this.AcceptTagInput();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            await this.AddTagAndUpdate(tag).ConfigureAwait(false);
        }

        private async void AddSelectedTagMenuItem_Click(object sender, EventArgs e)
        {
            await this.AddSelectedTag().ConfigureAwait(true);
        }

        private async Task AddTagAndUpdate(string tag)
        {
            foreach (var searchResult in this.searchResults)
            {
                await this.index.AddHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(true);
            }

            if (!this.tagCounts.ContainsKey(tag))
            {
                this.tagsInEntryOrder.Add(tag);
            }

            this.tagCounts[tag] = this.searchResults.Count;
            this.rejectedCounts.Remove(tag);
            this.rejectedInEntryOrder.Remove(tag);

            if (!this.IsDisposed)
            {
                this.RefreshTags();

                if (!this.IsDisposed)
                {
                    this.tagLayoutPanel.ScrollControlIntoView(this.existingTags.Controls.OfType<TagControl>().Where(c => c.Text == tag && !c.Negated).Single());
                }
            }
        }

        private void AddTagsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private Dictionary<string, int> CountTags(Func<SearchResult, IEnumerable<string>> getTags)
        {
            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in this.searchResults.SelectMany(getTags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            return tagCounts;
        }

        private async void EditTagsForm_Load(object sender, EventArgs e)
        {
            this.Enabled = false;
            await Task.Delay(1).ConfigureAwait(true);
            await this.PopulateExistingTags().ConfigureAwait(true);
            this.PopulateTagsCombo();
            this.Enabled = true;
        }

        private async Task PopulateExistingTags()
        {
            var tagComparer = this.index.TagEngine.GetTagComparer();
            this.tagCounts = this.CountTags(r => r.Tags);
            this.tagsInEntryOrder.AddRange(this.tagCounts.Keys.OrderBy(t => t, tagComparer));
            this.rejectedCounts = this.CountTags(r => r.RejectedTags);
            this.rejectedInEntryOrder.AddRange(this.rejectedCounts.Keys.OrderBy(t => t, tagComparer));

            this.RefreshTags();
        }

        private async void PopulateTagsCombo()
        {
            var engine = this.index.TagEngine;
            var rawTags = await this.index.GetAllHashTags().ConfigureAwait(true);
            var tags = engine.GetKnownTags().Concat(rawTags).Select(engine.Rename).Distinct().Select(t => engine[t]);
            var text = this.tagSearchBox.Text;
            this.tagSearchBox.Engine = engine;
            this.tagSearchBox.Items = tags.ToList();
            this.tagSearchBox.Text = text;
        }

        private void RefreshTags()
        {
            this.toolTip.Hide(this);
            var threshold = this.searchResults.Count / 2 + 1;
            var tagsByVote = this.tagCounts.Where(t => t.Value >= threshold).Select(t => t.Key);
            var rejectedByVote = this.rejectedCounts.Where(t => t.Value >= threshold).Select(t => t.Key);
            var analysisResult = this.index.TagEngine.Analyze(tagsByVote, rejectedByVote);
            var allMissingTags = new HashSet<string>(analysisResult.MissingTagSets.SelectMany(t => t.Result));
            var missingTags = new HashSet<string>(analysisResult.MissingTagSets.Where(t => t.Result.Count == 1).SelectMany(t => t.Result));

            var allTagObjects = new List<(bool rejected, string tag)>(this.tagCounts.Count + this.rejectedCounts.Count);
            allTagObjects.AddRange(
                this.tagsInEntryOrder.Select(t => (false, t)));
            allTagObjects.AddRange(
                this.rejectedInEntryOrder.Select(t => (true, t)));

            this.existingTags.UpdateControlsCollection(
                allTagObjects,
                pair => ControlHelpers.Construct<TagControl>(t =>
                {
                    t.AllowDelete = true;
                    t.Cursor = Cursors.Hand;
                    t.DeleteClick += this.TagControl_DeleteClick;
                }),
                (tagControl, pair) =>
                {
                    var tag = pair.tag;
                    var rejected = pair.rejected;
                    var indeterminate = (rejected ? this.rejectedCounts : this.tagCounts)[tag] != this.searchResults.Count;
                    var error = analysisResult.ExistingRejectedTags.Contains(tag);
                    tagControl.Text = tag;
                    tagControl.Tag = pair;
                    tagControl.Indeterminate = indeterminate;
                    tagControl.Negated = rejected;
                    tagControl.ContextMenuStrip = rejected ? this.rejectContextMenu : this.tagContextMenu;
                    tagControl.TagColor = error ? Color.Red : default(Color?);
                },
                tagControl =>
                {
                    tagControl.DeleteClick -= this.TagControl_DeleteClick;
                    tagControl.Dispose();
                });

            Application.DoEvents();
            var rulesLookup = analysisResult.SuggestedTags.ToLookup(r => r.Result, r => r.Rules);
            this.suggestedTags.UpdateControlsCollection(
                rulesLookup.OrderByDescending(g => missingTags.Contains(g.Key)).ThenByDescending(g => g.Count()).ToList(),
                tag => ControlHelpers.Construct<TagControl>(t =>
                {
                    t.AllowDelete = true;
                    t.ContextMenuStrip = this.suggestionContextMenu;
                    t.Cursor = Cursors.Hand;
                    t.MouseClick += this.SuggestionControl_MouseClick;
                    t.DeleteClick += this.SuggestionControl_DeleteClick;
                    t.MouseEnter += this.SuggestionControl_MouseEnter;
                    t.MouseLeave += this.SuggestionControl_MouseLeave;
                }),
                (tagControl, tag) =>
                {
                    var toolTip = string.Join(Environment.NewLine, tag.Select(r => string.Join(Environment.NewLine, r)));
                    var isMissing = missingTags.Contains(tag.Key);
                    var isInMissingGroups = allMissingTags.Contains(tag.Key);

                    tagControl.Text = tag.Key;
                    tagControl.Tag = toolTip;
                    tagControl.Indeterminate = !isMissing; // TODO: Third state for !isInMissingGroups,
                },
                tagControl =>
                {
                    tagControl.MouseClick -= this.SuggestionControl_MouseClick;
                    tagControl.DeleteClick -= this.SuggestionControl_DeleteClick;
                    tagControl.MouseEnter -= this.SuggestionControl_MouseEnter;
                    tagControl.MouseLeave -= this.SuggestionControl_MouseLeave;
                    this.toolTip.SetToolTip(tagControl, null);
                    tagControl.Dispose();
                });
        }

        private async void RejectSelectedTagMenuItem_Click(object sender, EventArgs e)
        {
            var tag = this.AcceptTagInput();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            // TODO: Prompt to remove existing children.
            await this.RejectTagAndUpdate(tag).ConfigureAwait(true);
        }

        private async void RejectSuggestionMenuItem_Click(object sender, EventArgs e)
        {
            var tag = (string)((ToolStripMenuItem)sender).Tag;
            await this.RejectTagAndUpdate(tag).ConfigureAwait(true);
        }

        private async Task RejectTagAndUpdate(string tag)
        {
            foreach (var searchResult in this.searchResults)
            {
                await this.index.RemoveHashTag(new HashTag(searchResult.Hash, tag), rejectTag: true).ConfigureAwait(true);
            }

            if (!this.rejectedCounts.ContainsKey(tag))
            {
                this.rejectedInEntryOrder.Add(tag);
            }

            this.rejectedCounts[tag] = this.searchResults.Count;
            this.tagCounts.Remove(tag);
            this.tagsInEntryOrder.Remove(tag);
            this.RefreshTags();
        }

        private async void RejectTagMenuItem_Click(object sender, EventArgs e)
        {
            var tagControl = (TagControl)((sender as ToolStripMenuItem)?.Owner as ContextMenuStrip)?.SourceControl;
            var tag = tagControl.Text;
            await this.RejectTagAndUpdate(tag).ConfigureAwait(true);
        }

        private void RemoveTagMenuItem_Click(object sender, EventArgs e)
        {
            var context = ((sender as ToolStripMenuItem)?.Owner as ContextMenuStrip)?.SourceControl;
            this.TagControl_DeleteClick(context, e);
        }

        private async void AddTagMenuItem_Click(object sender, EventArgs e)
        {
            var tagControl = (TagControl)((sender as ToolStripMenuItem)?.Owner as ContextMenuStrip)?.SourceControl;
            var tag = tagControl.Text;
            await this.AddTagAndUpdate(tag).ConfigureAwait(true);
        }

        private void CancelRejectionMenuItem_Click(object sender, EventArgs e)
        {
            var context = ((sender as ToolStripMenuItem)?.Owner as ContextMenuStrip)?.SourceControl;
            this.TagControl_DeleteClick(context, e);
        }

        private async void SuggestionControl_DeleteClick(object sender, EventArgs e)
        {
            var tagControl = (TagControl)sender;
            var tag = tagControl.Text;
            await this.RejectTagAndUpdate(tag).ConfigureAwait(true);
        }

        private async void SuggestionControl_MouseClick(object sender, MouseEventArgs e)
        {
            var tagControl = (TagControl)sender;
            var tag = tagControl.Text;
            this.tagSearchBox.Focus();

            await this.AddTagAndUpdate(tag).ConfigureAwait(true);
        }

        private void SuggestionControl_MouseEnter(object sender, EventArgs e)
        {
            if (sender is TagControl tagControl)
            {
                if (tagControl.Tag is string toolTip)
                {
                    var location = tagControl.PointToScreen(new Point(0, tagControl.Height));
                    location.Offset(-this.Left, -this.Top + 1);
                    this.toolTip.Show(toolTip, this, location);
                }
                else
                {
                    this.toolTip.Hide(this);
                }
            }
        }

        private void SuggestionControl_MouseLeave(object sender, EventArgs e)
        {
            if (sender is TagControl tagControl)
            {
                var cursorPosition = Cursor.Position;
                var screenRectangle = tagControl.RectangleToScreen(new Rectangle(0, 0, tagControl.Width, tagControl.Height));
                if (!screenRectangle.Contains(cursorPosition))
                {
                    this.toolTip.Hide(this);
                }
            }
        }

        private async void TagControl_DeleteClick(object sender, EventArgs e)
        {
            var tagControl = (TagControl)sender;
            var (rejected, tag) = ((bool, string))tagControl.Tag;

            if (rejected)
            {
                foreach (var searchResult in this.searchResults)
                {
                    await this.index.RemoveRejectedHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(true);
                }

                this.rejectedCounts.Remove(tag);
                this.rejectedInEntryOrder.Remove(tag);
            }
            else
            {
                foreach (var searchResult in this.searchResults)
                {
                    await this.index.RemoveHashTag(new HashTag(searchResult.Hash, tag)).ConfigureAwait(true);
                }

                this.tagCounts.Remove(tag);
                this.tagsInEntryOrder.Remove(tag);
            }

            this.RefreshTags();
        }

        private void TagSearchBox_TextUpdate(object sender, EventArgs e)
        {
            var hasText = !string.IsNullOrEmpty(this.tagSearchBox.Text);
            this.addSelectedTagMenuItem.Enabled = hasText;
            this.rejectSelectedTagMenuItem.Enabled = hasText;
        }

        private void SuggestionContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender == this.suggestionContextMenu && this.suggestionContextMenu.SourceControl is TagControl tagControl)
            {
                var tag = tagControl.Text;
                var info = this.index.TagEngine[tag];
                var tagList = info.Ancestors.OrderBy(a => a, StringComparer.CurrentCultureIgnoreCase).ToList();

                this.rejectSuggestionMenuItem.Tag = tag;
                this.suggestionContextMenu.Items.UpdateComponentsCollection(
                    1,
                    this.suggestionContextMenu.Items.Count - 1,
                    tagList,
                    savedSearch => ControlHelpers.Construct<ToolStripMenuItem>(suggestion =>
                    {
                        suggestion.Click += this.RejectSuggestionMenuItem_Click;
                    }),
                    (suggestion, ancestor) =>
                    {
                        suggestion.Text = $"Reject '{ancestor}'";
                        suggestion.Tag = ancestor;
                    },
                    suggestion =>
                    {
                        suggestion.Click -= this.RejectSuggestionMenuItem_Click;
                    });
            }
        }
    }
}
