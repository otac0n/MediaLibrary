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
            var tagComparer = this.index.TagEngine.GetTagComparer();

            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            this.SuspendLayout();

            if (tagCounts.Count == 0)
            {
                foreach (var control in this.Controls)
                {
                    (control as IDisposable)?.Dispose();
                }

                this.Controls.Clear();
            }
            else
            {
                var write = 0;
                foreach (var tag in tagCounts.OrderBy(t => t.Key, tagComparer))
                {
                    TagControl tagControl;

                    var updated = false;
                    while (write < this.Controls.Count)
                    {
                        tagControl = (TagControl)this.Controls[write];
                        var comp = tagComparer.Compare(tagControl.Text, tag.Key);
                        if (comp < 0)
                        {
                            this.Controls.RemoveAt(write);
                            tagControl.Dispose();
                        }
                        else if (comp == 0)
                        {
                            tagControl.TagColor = this.index.TagEngine.GetTagColor(tag.Key);
                            tagControl.Indeterminate = tag.Value != searchResults.Count;
                            write++;
                            updated = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!updated)
                    {
                        tagControl = null;
                        try
                        {
                            tagControl = new TagControl();
                            tagControl.AllowDelete = false;
                            tagControl.Text = tag.Key;
                            tagControl.TagColor = this.index.TagEngine.GetTagColor(tag.Key);
                            tagControl.Indeterminate = tag.Value != searchResults.Count;

                            this.Controls.Add(tagControl);
                            this.Controls.SetChildIndex(tagControl, write++);
                            tagControl = null;
                        }
                        finally
                        {
                            tagControl?.Dispose();
                        }
                    }
                }
            }

            this.ResumeLayout();
        }
    }
}
