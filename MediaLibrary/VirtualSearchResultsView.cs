// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using ByteSizeLib;
    using MediaLibrary.Properties;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public class VirtualSearchResultsView : ListView
    {
        private readonly Dictionary<string, ColumnHeader> columns;
        private readonly MediaIndex index;
        private readonly Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        private readonly VirtualListSorter sorter;
        private bool columnsAutoSized = false;
        public VirtualSearchResultsView(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.AllowColumnReorder = true;
            this.FullRowSelect = true;
            this.HideSelection = false;
            this.View = View.Details;

            this.ListViewItemSorter = this.sorter = new VirtualListSorter();

            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;
            this.index.HashPersonAdded += this.Index_HashPersonAdded;
            this.index.HashPersonRemoved += this.Index_HashPersonRemoved;
            this.index.RatingUpdated += this.Index_RatingUpdated;

            this.columns = new Dictionary<string, ColumnHeader>();
            foreach (var column in Enum.GetValues(typeof(Column)).Cast<Column>())
            {
                var columnName = column.ToString();
                var columnHeader = this.columns[columnName] = new ColumnHeader();
                columnHeader.Name = columnName;
                columnHeader.Text = Resources.ResourceManager.GetString($"{columnName}Column", CultureInfo.CurrentCulture);
                this.Columns.Add(columnHeader);

                // TODO: this.fileSizeColumn.TextAlign = HorizontalAlignment.Right;
                // TODO: this.ratingColumn.TextAlign = HorizontalAlignment.Right;
            }

            this.ColumnClick += this.Internal_ColumnClick;
        }

        private enum Column
        {
            Name,
            Path,
            People,
            Tags,
            FileSize,
            Rating,
        }

        public string ColumnsSettings
        {
            get => string.Join(",", this.Columns.Cast<ColumnHeader>().OrderBy(c => c.DisplayIndex).Select(h => this.columnsAutoSized ? $"{h.Name}:{h.Width}" : h.Name));

            set
            {
                var columnsDesc = (value ?? string.Empty).Split(",; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                this.BeginUpdate();

                var displayIndex = 0;
                foreach (var desc in columnsDesc)
                {
                    var parts = desc.Split(new[] { ':' }, 2);
                    if (this.columns.TryGetValue(parts[0], out var columnHeader))
                    {
                        var width = parts.Length > 1 && int.TryParse(parts[1], out var widthVal) && widthVal > 0
                            ? widthVal
                            : default(int?);

                        columnHeader.DisplayIndex = displayIndex++;
                        if (width != null)
                        {
                            columnHeader.Width = width.Value;
                            this.columnsAutoSized = true;
                        }
                    }
                }

                this.EndUpdate();
            }
        }

        public IList<SearchResult> SearchResults
        {
            set
            {
                var newHashes = new HashSet<string>(value.Select(i => i.Hash));

                this.ListViewItemSorter = null;

                for (var i = this.Items.Count - 1; i >= 0; i--)
                {
                    var item = this.Items[i];
                    var hash = ((SearchResult)item.Tag).Hash;
                    if (!newHashes.Contains(hash))
                    {
                        this.items.Remove(hash);
                        this.Items.RemoveAt(i);
                    }
                }

                foreach (var item in value)
                {
                    if (!this.items.TryGetValue(item.Hash, out var _))
                    {
                        this.Items.Add(this.CreateListItem(item));
                    }
                }

                if (!this.columnsAutoSized && value.Count > 0)
                {
                    this.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    this.columnsAutoSized = true;
                }

                this.ListViewItemSorter = this.sorter;
                this.Sort();
            }
        }

        public string SortColumn
        {
            get => this.sorter.SortColumn.ToString();
            set => this.sorter.SortColumn = (Column)Enum.Parse(typeof(Column), value, ignoreCase: true);
        }

        public bool SortDescending
        {
            get => this.sorter.Descending;
            set => this.sorter.Descending = value;
        }

        private static string GetImageKey(string fileType)
        {
            switch (fileType)
            {
                case "audio/aac": return "audio-file-aac";
                case "audio/midi": return "audio-file-midi";
                case "audio/mpeg": return "audio-file-mp3";
                case "audio/wav": return "audio-file-wav";
                case "audio/x-aiff": return "audio-file-aif";
                case "audio":
                case string type when type.StartsWith("audio/", StringComparison.InvariantCulture):
                    return "audio-file";

                case "image/bmp": return "image-file-bmp";
                case "image/gif": return "image-file-gif";
                case "image/jpeg": return "image-file-jpg";
                case "image/png": return "image-file-png";
                case "image/tiff": return "image-file-tiff";
                case "image":
                case string type when type.StartsWith("image/", StringComparison.InvariantCulture):
                    return "image-file";

                case "video/mp4": return "video-file-mp4";
                case "video/mpeg": return "video-file-mpg";
                case "video/quicktime": return "video-file-qt";
                case "video/webm": return "video-file-m4v";
                case "video/x-flv": return "video-file-flv";
                case "video/x-msvideo": return "video-file-avi";
                case "video":
                case string type when type.StartsWith("video/", StringComparison.InvariantCulture):
                    return "video-file";

                default: return "common-file";
            }
        }

        private static void UpdateListItemPath(ListViewItem item, SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();
            item.SubItems[(int)Column.Path].Text = firstPath != null ? Path.GetDirectoryName(firstPath) : string.Empty;
            item.SubItems[(int)Column.Path].Tag = firstPath;
            item.SubItems[(int)Column.Name].Text = firstPath != null ? Path.GetFileNameWithoutExtension(firstPath) : searchResult.Hash;
            item.SubItems[(int)Column.Name].Tag = firstPath;
        }

        private static void UpdateListItemPeople(ListViewItem item, SearchResult searchResult)
        {
            item.SubItems[(int)Column.People].Text = string.Join("; ", searchResult.People.Select(p => p.Name));
            item.SubItems[(int)Column.People].Tag = searchResult.People;
        }

        private static void UpdateListItemRating(ListViewItem item, SearchResult searchResult)
        {
            var rating = searchResult.Rating;
            item.SubItems[(int)Column.Rating].Text = rating != null ? $"{Math.Round(rating.Value)}{(rating.Count < 15 ? "?" : string.Empty)}" : string.Empty;
            item.SubItems[(int)Column.Rating].Tag = rating;
        }

        private static void UpdateListItemTags(ListViewItem item, SearchResult searchResult)
        {
            item.SubItems[(int)Column.Tags].Text = string.Join("; ", searchResult.Tags);
            item.SubItems[(int)Column.Tags].Tag = searchResult.Tags;
        }

        private ListViewItem CreateListItem(SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

            var columns = new ListViewItem.ListViewSubItem[6];
            for (var i = 0; i < columns.Length; i++)
            {
                columns[i] = new ListViewItem.ListViewSubItem();
            }

            var item = new ListViewItem(columns, GetImageKey(searchResult.FileType))
            {
                Tag = searchResult,
            };

            UpdateListItemPath(item, searchResult);
            UpdateListItemPeople(item, searchResult);
            UpdateListItemTags(item, searchResult);
            UpdateListItemRating(item, searchResult);

            columns[(int)Column.FileSize].Text = ByteSize.FromBytes(searchResult.FileSize).ToString();
            columns[(int)Column.FileSize].Tag = searchResult.FileSize;

            return this.items[searchResult.Hash] = item;
        }

        private void Index_HashPersonAdded(object sender, ItemAddedEventArgs<(HashPerson hash, Person person)> e)
        {
            this.UpdateSearchResult(e.Item.hash.Hash, UpdateListItemPeople);
        }

        private void Index_HashPersonRemoved(object sender, ItemRemovedEventArgs<HashPerson> e)
        {
            this.UpdateSearchResult(e.Item.Hash, UpdateListItemPeople);
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, UpdateListItemTags);
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, UpdateListItemTags);
        }

        private void Index_RatingUpdated(object sender, ItemUpdatedEventArgs<Rating> e)
        {
            this.UpdateSearchResult(e.Item.Hash, UpdateListItemRating);
        }

        private void Internal_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var column = this.Columns[e.Column];
            if ((int)this.sorter.SortColumn != column.Index)
            {
                this.sorter.SortColumn = (Column)column.Index;
                this.sorter.Descending = false;
            }
            else
            {
                this.sorter.Descending = !this.sorter.Descending;
            }

            // TODO: Raise sort changed event to notify settings to update.
            this.Sort();
        }

        private void UpdateSearchResult(string hash, Action<ListViewItem, SearchResult> updateListViewItem)
        {
            if (this.items.TryGetValue(hash, out var item))
            {
                var searchResult = (SearchResult)item.Tag;
                this.InvokeIfRequired(() => updateListViewItem(item, searchResult));
            }
        }

        private class VirtualListSorter : IComparer<ListViewItem>, IComparer
        {
            public bool Descending { get; set; }

            public Column SortColumn { get; set; }

            public int Compare(ListViewItem a, ListViewItem b)
            {
                int value;
                switch (this.SortColumn)
                {
                    case Column.Name:
                        value = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[(int)Column.Name].Text, b.SubItems[(int)Column.Name].Text);
                        break;

                    case Column.Path:
                        value = PathComparer.Instance.Compare((string)a.SubItems[(int)Column.Name].Tag, (string)b.SubItems[(int)Column.Name].Tag);
                        break;

                    case Column.Tags:
                        value = ((ImmutableHashSet<string>)a.SubItems[(int)Column.Tags].Tag).Count.CompareTo(((ImmutableHashSet<string>)b.SubItems[(int)Column.Tags].Tag).Count);
                        break;

                    case Column.FileSize:
                        value = ((long)a.SubItems[(int)Column.FileSize].Tag).CompareTo((long)b.SubItems[(int)Column.FileSize].Tag);
                        break;

                    case Column.People:
                        value = ((ImmutableHashSet<Person>)a.SubItems[(int)Column.People].Tag).Count.CompareTo(((ImmutableHashSet<Person>)b.SubItems[(int)Column.People].Tag).Count);
                        break;

                    case Column.Rating:
                        {
                            var aRating = (Rating)a.SubItems[(int)Column.Rating].Tag;
                            var bRating = (Rating)b.SubItems[(int)Column.Rating].Tag;
                            value = (bRating?.Value ?? Rating.DefaultRating).CompareTo(aRating?.Value ?? Rating.DefaultRating);
                            if (value == 0)
                            {
                                value = (bRating?.Count ?? 0).CompareTo(aRating?.Count ?? 0);
                            }
                        }

                        break;

                    default:
                        value = 0;
                        break;
                }

                return
                    !this.Descending || value == 0 ? value :
                    value > 0 ? -1 : 1;
            }

            public int Compare(object a, object b) => this.Compare(a as ListViewItem, b as ListViewItem);
        }
    }
}
