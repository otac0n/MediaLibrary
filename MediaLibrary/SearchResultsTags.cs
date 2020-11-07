// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public class SearchResultsTags : FlowLayoutPanel
    {
        private readonly MediaIndex index;
        private ImmutableList<SearchResult> searchResults;

        public SearchResultsTags(MediaIndex index)
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

            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            this.SuspendLayout();

            for (var i = 0; i < this.Controls.Count; i++)
            {
                var tagControl = (TagControl)this.Controls[i];
                var tag = tagControl.Text;
                if (tagCounts.TryGetValue(tag, out var count))
                {
                    tagControl.Indeterminate = count != searchResults.Count;
                    tagControl.TagColor = this.index.TagEngine.GetTagColor(tag);
                    tagCounts.Remove(tag);
                }
                else
                {
                    this.Controls.RemoveAt(i--);
                    tagControl.Dispose();
                }
            }

            foreach (var tag in tagCounts)
            {
                this.Controls.Add(new TagControl
                {
                    AllowDelete = false,
                    Text = tag.Key,
                    TagColor = this.index.TagEngine.GetTagColor(tag.Key),
                    Indeterminate = tag.Value != searchResults.Count,
                });
            }

            this.ResumeLayout();
        }
    }
}
