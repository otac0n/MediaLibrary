// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Properties;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public class SearchResultsTags : FlowLayoutPanel
    {
        private readonly IMediaIndex index;
        private ImmutableList<SearchResult> searchResults;

        public SearchResultsTags(IMediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        [DefaultValue(true)]
        public override bool AutoSize { get => base.AutoSize; set => base.AutoSize = value; }

        [DefaultValue(AutoSizeMode.GrowAndShrink)]
        public override AutoSizeMode AutoSizeMode { get => base.AutoSizeMode; set => base.AutoSizeMode = value; }

        [Category("Data")]
        public IList<SearchResult> SearchResults
        {
            get => this.searchResults;

            set
            {
                this.searchResults = value?.ToImmutableList() ?? ImmutableList<SearchResult>.Empty;
                this.UpdateTags();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.index.HashTagAdded -= this.Index_HashTagAdded;
                this.index.HashTagRemoved -= this.Index_HashTagRemoved;
            }

            base.Dispose(disposing);
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void UpdateSearchResult(string hash)
        {
            if (this.searchResults.Any(r => r.Hash == hash))
            {
                this.InvokeIfRequired(() => this.UpdateTags());
            }
        }

        private void UpdateTags()
        {
            var searchResults = this.searchResults;
            var tagComparer = this.index.TagEngine.GetTagComparer();

            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            this.UpdateControlsCollection(
                tagCounts.Keys.OrderByDescending(k => k == "favorite").ThenBy(k => k, tagComparer).ToList(),
                tag => tag == "favorite"
                    ? (Control)ControlHelpers.Construct<PictureBox>(p => p.SizeMode = PictureBoxSizeMode.Zoom)
                    : (Control)ControlHelpers.Construct<TagControl>(t => t.AllowDelete = false),
                (control, tag) => tag == "favorite"
                    ? control is PictureBox
                    : control is TagControl,
                (control, tag) =>
                {
                    var indeterminate = tagCounts[tag] != searchResults.Count;
                    if (tag == "favorite")
                    {
                        var pictureBox = (PictureBox)control;
                        var fontHeight = (int)Math.Round(this.Font.GetHeight()) + 3 * 2;
                        pictureBox.Image = indeterminate ? Resources.love_it : Resources.love_it_filled;
                        pictureBox.Width = fontHeight;
                        pictureBox.Height = fontHeight;
                    }
                    else
                    {
                        var tagControl = (TagControl)control;
                        tagControl.Text = tag;
                        tagControl.Tag = tag;
                        tagControl.Indeterminate = indeterminate;
                        tagControl.TagColor = this.index.TagEngine.GetTagColor(tag);
                    }
                },
                control =>
                {
                    control.Dispose();
                });
        }
    }
}
