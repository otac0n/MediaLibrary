// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Properties;
    using MediaLibrary.Services;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public class SearchResultsVectors : FlowLayoutPanel
    {
        private readonly MediaIndex index;
        private ImmutableList<SearchResult> searchResults;

        public SearchResultsVectors(MediaIndex index)
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

        private IEnumerable<(object key, bool indeterminate)> GetVectorsInOrder()
        {
            var searchResults = this.searchResults;
            var tagComparer = this.index.TagEngine.GetTagComparer();

            var tagCounts = new Dictionary<string, int>();
            foreach (var tag in searchResults.SelectMany(r => r.Tags))
            {
                tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            var people = new Dictionary<int, Person>();
            var personCounts = new Dictionary<int, int>();
            foreach (var person in this.searchResults.SelectMany(r => r.People))
            {
                people[person.PersonId] = person;
                personCounts[person.PersonId] = personCounts.TryGetValue(person.PersonId, out var count) ? count + 1 : 1;
            }

            if (tagCounts.TryGetValue(TagComparer.FavoriteTag, out var favoriteCount))
            {
                yield return (TagComparer.FavoriteTag, favoriteCount != searchResults.Count);
            }

            foreach (var person in personCounts)
            {
                yield return (people[person.Key], person.Value != this.searchResults.Count);
            }

            foreach (var tag in tagCounts.Where(t => t.Key != TagComparer.FavoriteTag).OrderBy(t => t.Key, tagComparer))
            {
                yield return (tag.Key, tag.Value != searchResults.Count);
            }
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
            var vectors = this.GetVectorsInOrder().ToList();

            this.UpdateControlsCollection(
                vectors,
                vector => vector.key is string tag
                    ? tag == TagComparer.FavoriteTag
                        ? (Control)ControlHelpers.Construct<PictureBox>(p => p.SizeMode = PictureBoxSizeMode.Zoom)
                        : (Control)ControlHelpers.Construct<TagControl>(t => t.AllowDelete = false)
                    : (Control)ControlHelpers.Construct<PersonControl>(t => t.AllowDelete = false),
                (control, vector) => vector.key is string tag
                    ? tag == TagComparer.FavoriteTag
                        ? control is PictureBox
                        : control is TagControl
                    : control is PersonControl,
                (control, vector) =>
                {
                    if (vector.key is string tag)
                    {
                        if (tag == TagComparer.FavoriteTag)
                        {
                            var pictureBox = (PictureBox)control;
                            var fontHeight = (int)Math.Round(this.Font.GetHeight()) + 3 * 2;
                            pictureBox.Image = vector.indeterminate ? Resources.love_it : Resources.love_it_filled;
                            pictureBox.Width = fontHeight;
                            pictureBox.Height = fontHeight;
                        }
                        else
                        {
                            var tagControl = (TagControl)control;
                            tagControl.Text = tag;
                            tagControl.Tag = tag;
                            tagControl.Indeterminate = vector.indeterminate;
                            tagControl.TagColor = this.index.TagEngine.GetTagColor(tag);
                        }
                    }
                    else
                    {
                        var person = (Person)vector.key;
                        var personControl = (PersonControl)control;
                        personControl.Person = person;
                        personControl.Indeterminate = vector.indeterminate;
                    }
                },
                control =>
                {
                    control.Dispose();
                });
        }
    }
}
