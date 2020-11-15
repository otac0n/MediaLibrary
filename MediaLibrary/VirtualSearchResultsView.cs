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
        private static readonly ImmutableDictionary<Column, ColumnDefinition> ColumnDefinitions = new ColumnDefinitionList
        {
            {
                Column.Path,
                r =>
                {
                    var path = GetBestPath(r);
                    return path == null ? string.Empty : Path.GetDirectoryName(path);
                },
                (a, b) => PathComparer.Instance.Compare(GetBestPath(a), GetBestPath(b))
            },
            {
                Column.Name,
                r =>
                {
                    var path = GetBestPath(r);
                    return path == null ? string.Empty : Path.GetFileNameWithoutExtension(path);
                },
                (a, b) =>
                {
                    var aPath = GetBestPath(a);
                    var bPath = GetBestPath(b);
                    return StringComparer.CurrentCultureIgnoreCase.Compare(
                        aPath == null ? null : Path.GetFileNameWithoutExtension(aPath),
                        bPath == null ? null : Path.GetFileNameWithoutExtension(bPath));
                }
            },
            { Column.People, r => string.Join("; ", r.People.Select(p => p.Name)), (a, b) => a.People.Count.CompareTo(b.People.Count) },
            { Column.Tags, r => string.Join("; ", r.Tags), (a, b) => a.Tags.Count.CompareTo(b.Tags.Count) },
            {
                Column.FileSize,
                r => ByteSize.FromBytes(r.FileSize).ToString(),
                (a, b) => a.FileSize.CompareTo(b.FileSize),
                HorizontalAlignment.Right
            },
            {
                Column.Rating,
                r => r.Rating != null ? $"{Math.Round(r.Rating.Value)}{(r.Rating.Count < 15 ? "?" : string.Empty)}" : string.Empty,
                (a, b) =>
                {
                    var aRating = a.Rating;
                    var bRating = b.Rating;
                    var value = (bRating?.Value ?? Rating.DefaultRating).CompareTo(aRating?.Value ?? Rating.DefaultRating);
                    if (value == 0)
                    {
                        value = (bRating?.Count ?? 0).CompareTo(aRating?.Count ?? 0);
                    }

                    return value;
                },
                HorizontalAlignment.Right
            },
        }.ToImmutableDictionary(c => c.Column);

        private readonly Dictionary<Column, ColumnHeader> columns = new Dictionary<Column, ColumnHeader>();
        private readonly MediaIndex index;
        private readonly Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        private readonly List<SearchResult> orderdResults = new List<SearchResult>();
        private readonly VirtualListSorter sorter;
        private bool columnsSized = false;

        public VirtualSearchResultsView(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            base.VirtualMode = true;
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

            foreach (var column in ColumnDefinitions.Values.OrderBy(c => c.Index))
            {
                var columnHeader = this.columns[column.Column] = new ColumnHeader();
                columnHeader.Name = column.Column.ToString();
                columnHeader.Text = column.Name;
                columnHeader.TextAlign = column.HorizontalAlignment;
                this.Columns.Add(columnHeader);
            }

            this.ColumnWidthChanged += this.Internal_ColumnWidthChanged;
            this.ColumnClick += this.Internal_ColumnClick;
            this.CacheVirtualItems += this.Internal_CacheVirtualItems;
            this.RetrieveVirtualItem += this.Internal_RetrieveVirtualItem;
            this.SearchForVirtualItem += this.Internal_SearchForVirtualItem;
        }

        private enum Column
        {
            Path,
            Name,
            People,
            Tags,
            FileSize,
            Rating,
        }

        public string ColumnsSettings
        {
            get => string.Join(",", this.Columns.Cast<ColumnHeader>().OrderBy(c => c.DisplayIndex).Select(h => this.columnsSized ? $"{h.Name}:{h.Width}" : h.Name));

            set
            {
                var columnsDesc = (value ?? string.Empty).Split(",; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                this.BeginUpdate();

                var displayIndex = 0;
                foreach (var desc in columnsDesc)
                {
                    var parts = desc.Split(new[] { ':' }, 2);
                    if (Enum.TryParse<Column>(parts[0], out var column) && this.columns.TryGetValue(column, out var columnHeader))
                    {
                        var width = parts.Length > 1 && int.TryParse(parts[1], out var widthVal) && widthVal > 0
                            ? widthVal
                            : default(int?);

                        columnHeader.DisplayIndex = displayIndex++;
                        if (width != null)
                        {
                            columnHeader.Width = width.Value;
                        }
                    }
                }

                this.EndUpdate();
            }
        }

        public IList<SearchResult> SearchResults
        {
            get
            {
                return this.orderdResults.AsReadOnly();
            }

            set
            {
                var keepHashes = new HashSet<string>(value.Select(v => v.Hash));
                foreach (var key in this.items.Keys.ToList())
                {
                    if (!keepHashes.Contains(key))
                    {
                        this.items.Remove(key);
                    }
                }

                this.orderdResults.Clear();
                this.orderdResults.AddRange(value);
                this.Resort();
                this.VirtualListSize = this.orderdResults.Count;
                this.Invalidate();
            }
        }

        public IList<SearchResult> SelectedResults =>
            this.SelectedIndices.Cast<int>().Select(i => this.orderdResults[i]).ToList();

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

        public new bool VirtualMode => base.VirtualMode;

        private static string GetBestPath(SearchResult searchResult) => searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

        private static string GetImageKey(string fileType)
        {
            switch (fileType)
            {
                case "audio":
                case string type when type.StartsWith("audio/", StringComparison.InvariantCulture):
                    return "audio-file";

                case "image/gif":
                    return "video-file";

                case "image":
                case string type when type.StartsWith("image/", StringComparison.InvariantCulture):
                    return "image-file";

                case "video":
                case string type when type.StartsWith("video/", StringComparison.InvariantCulture):
                    return "modern-tv-flat";

                default: return "common-file";
            }
        }

        private static void UpdateListViewItem(ListViewItem item, SearchResult searchResult)
        {
            foreach (var column in ColumnDefinitions.Values)
            {
                UpdateListViewItemColumn(item, searchResult, column);
            }
        }

        private static void UpdateListViewItemColumn(ListViewItem item, SearchResult searchResult, ColumnDefinition column)
        {
            item.SubItems[column.Index].Text = column.GetValue(searchResult);
        }

        private ListViewItem CreateListItem(SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

            var columns = new ListViewItem.ListViewSubItem[6];
            for (var i = 0; i < columns.Length; i++)
            {
                columns[i] = new ListViewItem.ListViewSubItem();
            }

            var item = new ListViewItem(columns, this.GetImageIndex(GetImageKey(searchResult.FileType)))
            {
                Tag = searchResult,
            };

            UpdateListViewItem(item, searchResult);

            return this.items[searchResult.Hash] = item;
        }

        private int GetImageIndex(string key) =>
            (this.View == View.LargeIcon ? this.LargeImageList : this.SmallImageList)?.Images?.IndexOfKey(key) ?? -1;

        private ListViewItem GetOrCreateListItem(int index) => this.GetOrCreateListItem(this.orderdResults[index]);

        private ListViewItem GetOrCreateListItem(SearchResult searchResult)
        {
            var hash = searchResult.Hash;
            if (!this.items.TryGetValue(hash, out var listItem))
            {
                this.items[hash] = listItem = this.CreateListItem(searchResult);
            }

            return listItem;
        }

        private void Index_HashPersonAdded(object sender, ItemAddedEventArgs<(HashPerson hash, Person person)> e)
        {
            this.UpdateSearchResult(e.Item.hash.Hash, Column.People);
        }

        private void Index_HashPersonRemoved(object sender, ItemRemovedEventArgs<HashPerson> e)
        {
            this.UpdateSearchResult(e.Item.Hash, Column.People);
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, Column.Tags);
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, Column.Tags);
        }

        private void Index_RatingUpdated(object sender, ItemUpdatedEventArgs<Rating> e)
        {
            this.UpdateSearchResult(e.Item.Hash, Column.Rating);
        }

        private void Internal_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            for (var i = e.StartIndex; i <= e.EndIndex; i++)
            {
                this.GetOrCreateListItem(i);
            }
        }

        private void Internal_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var column = this.Columns[e.Column];
            if ((int)this.sorter.SortColumn != column.Index)
            {
                this.sorter.SortColumn = (Column)column.Index;
                this.sorter.Descending = false; // TODO: Default sort
            }
            else
            {
                this.sorter.Descending = !this.sorter.Descending;
            }

            // TODO: Raise sort changed event to notify settings to update.
            this.ResortMaintainingSelection();
            this.Invalidate();
        }

        private void Internal_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            this.columnsSized = true;
        }

        private void Internal_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item = this.GetOrCreateListItem(e.ItemIndex);
        }

        private void Internal_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            var indices = e.Direction == SearchDirectionHint.Down || e.Direction == SearchDirectionHint.Right
                ? Enumerable.Range(e.StartIndex, this.orderdResults.Count - e.StartIndex)
                : Enumerable.Range(0, e.StartIndex + 1).Reverse();

            var test = new Func<string, bool>(subject => subject.StartsWith(e.Text, StringComparison.CurrentCultureIgnoreCase));

            var found = false;
            var nameColumn = ColumnDefinitions[Column.Name];
            foreach (var ix in indices)
            {
                var result = this.orderdResults[ix];
                if (this.TryGetListItem(ix, out var listItem))
                {
                    found = test(listItem.SubItems[nameColumn.Index].Text);
                }
                else
                {
                    found = test(nameColumn.GetValue(result));
                }

                if (found)
                {
                    e.Index = ix;
                    break;
                }
            }
        }

        private void Resort()
        {
            this.orderdResults.Sort(this.sorter.Sorter.Compare);
        }

        private void ResortMaintainingSelection()
        {
            var selected = this.SelectedResults;
            this.Resort();
            this.SelectedIndices.Clear();
            foreach (var item in selected)
            {
                this.SelectedIndices.Add(this.orderdResults.IndexOf(item));
            }

            // TODO: Fix focus.
        }

        private bool TryGetListItem(int index, out ListViewItem listItem) => this.TryGetListItem(this.orderdResults[index], out listItem);

        private bool TryGetListItem(SearchResult searchResult, out ListViewItem listItem)
        {
            var hash = searchResult.Hash;
            return this.items.TryGetValue(hash, out listItem);
        }

        private void UpdateSearchResult(string hash, Column column)
        {
            if (this.items.TryGetValue(hash, out var item))
            {
                var searchResult = (SearchResult)item.Tag;
                var columnDefinition = ColumnDefinitions[column];
                this.InvokeIfRequired(() => UpdateListViewItemColumn(item, searchResult, columnDefinition));
            }
        }

        private class ColumnDefinition
        {
            public ColumnDefinition(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, string> getValue, Comparison<SearchResult> comparison = null)
                : this(column, horizontalAlignment, Resources.ResourceManager.GetString($"{column}Column", CultureInfo.CurrentCulture), (int)column, getValue, comparison)
            {
            }

            private ColumnDefinition(Column column, HorizontalAlignment horizontalAlignment, string name, int index, Func<SearchResult, string> getValue, Comparison<SearchResult> comparison = null)
            {
                this.Column = column;
                this.HorizontalAlignment = horizontalAlignment;
                this.Name = name ?? throw new ArgumentNullException(nameof(name));
                this.Index = index;
                this.GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
                this.Comparison = comparison ?? ((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(this.GetValue(a), this.GetValue(b)));
            }

            public Column Column { get; }

            public Comparison<SearchResult> Comparison { get; }

            public Func<SearchResult, string> GetValue { get; }

            public HorizontalAlignment HorizontalAlignment { get; }

            public int Index { get; }

            public string Name { get; }
        }

        private class ColumnDefinitionList : List<ColumnDefinition>
        {
            public void Add(Column column, Func<SearchResult, string> getValue, Comparison<SearchResult> comparison = null, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left) =>
                this.Add(new ColumnDefinition(column, horizontalAlignment, getValue, comparison));
        }

        private class SearchResultsSorter : IComparer<SearchResult>
        {
            public bool Descending { get; set; }

            public Column SortColumn { get; set; }

            public int Compare(SearchResult a, SearchResult b)
            {
                var column = ColumnDefinitions[this.SortColumn];
                var value = column.Comparison(a, b);

                return
                    !this.Descending || value == 0 ? value :
                    value > 0 ? -1 : 1;

                switch (this.SortColumn)
                {
                    case Column.Rating:
                        {
                            var aRating = a.Rating;
                            var bRating = b.Rating;
                            value = (bRating?.Value ?? Rating.DefaultRating).CompareTo(aRating?.Value ?? Rating.DefaultRating);
                            if (value == 0)
                            {
                                value = (bRating?.Count ?? 0).CompareTo(aRating?.Count ?? 0);
                            }
                        }

                        break;
                }
            }
        }

        private class VirtualListSorter : IComparer<ListViewItem>, IComparer
        {
            public VirtualListSorter()
            {
                this.Sorter = new SearchResultsSorter();
            }

            public bool Descending
            {
                get => this.Sorter.Descending;
                set => this.Sorter.Descending = value;
            }

            public Column SortColumn
            {
                get => this.Sorter.SortColumn;
                set => this.Sorter.SortColumn = value;
            }

            public SearchResultsSorter Sorter { get; }

            public int Compare(ListViewItem a, ListViewItem b) => this.Sorter.Compare((SearchResult)a.Tag, (SearchResult)b.Tag);

            public int Compare(object a, object b) => this.Compare(a as ListViewItem, b as ListViewItem);
        }
    }
}
