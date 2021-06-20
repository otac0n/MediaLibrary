// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using BrightIdeasSoftware;
    using ByteSizeLib;
    using MediaLibrary.Properties;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using TaggingLibrary;

    public class VirtualSearchResultsView : FastObjectListView
    {
        private readonly ImmutableDictionary<Column, ColumnDefinition> columnDefinitions;
        private readonly Dictionary<Column, OLVColumn> columns = new Dictionary<Column, OLVColumn>();
        private readonly IMediaIndex index;
        private readonly List<SearchResult> orderdResults = new List<SearchResult>();
        private bool columnsSized = false;
        private bool selectionChangedInvoked;
        private TagComparer tagComparer;

        public VirtualSearchResultsView(IMediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.AllowColumnReorder = true;
            this.FullRowSelect = true;
            this.View = View.Details;

            this.columnDefinitions = new ColumnDefinitionList
            {
                {
                    Column.Path,
                    GetBestPath,
                    value => value == null ? string.Empty : Path.GetDirectoryName(value),
                    (a, b) => PathComparer.Instance.Compare(GetBestPath(a), GetBestPath(b))
                },
                {
                    Column.Name,
                    r =>
                    {
                        var path = GetBestPath(r);
                        return path == null ? string.Empty : Path.GetFileNameWithoutExtension(path);
                    },
                    value => value,
                    (a, b) =>
                    {
                        var aPath = GetBestPath(a);
                        var bPath = GetBestPath(b);
                        return StringComparer.CurrentCultureIgnoreCase.Compare(
                            aPath == null ? null : Path.GetFileNameWithoutExtension(aPath),
                            bPath == null ? null : Path.GetFileNameWithoutExtension(bPath));
                    },
                    r => GetImageKey(r)
                },
                {
                    Column.People,
                    r => r.People,
                    value => string.Join("; ", value.Select(p => p.Name)),
                    (a, b) =>
                    {
                        int comp;
                        if ((comp = a.People.Count.CompareTo(b.People.Count)) != 0)
                        {
                            return comp;
                        }

                        var aPeople = a.People.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                        var bPeople = b.People.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                        for (var i = 0; i < aPeople.Count; i++)
                        {
                            if (aPeople[i].PersonId != bPeople[i].PersonId)
                            {
                                if ((comp = StringComparer.CurrentCultureIgnoreCase.Compare(aPeople[i].Name, bPeople[i].Name)) != 0)
                                {
                                    return comp;
                                }

                                return aPeople[i].PersonId.CompareTo(bPeople[i].PersonId);
                            }
                        }

                        return 0;
                    }
                },
                {
                    Column.Tags,
                    r => r.Tags,
                    value => string.Join("; ", value),
                    (a, b) => this.TagComparer.Compare(a.Tags, b.Tags),
                    (g, bounds, r) =>
                    {
                        var tagComparer = this.TagComparer;
                        var baseSize = g.MeasureString("#", this.Font);
                        var padding = (int)Math.Floor((bounds.Height - baseSize.Height) / 2);

                        var xOffset = 0f;
                        if (r.Tags.Contains("favorite"))
                        {
                            var size = bounds.Height - padding * 2;
                            g.DrawImage(Resources.love_it_filled, new RectangleF(bounds.Left + xOffset, bounds.Top + padding, size, size));
                            xOffset += size + padding;
                        }

                        foreach (var tag in r.Tags.Where(t => t != "favorite").OrderBy(t => t, tagComparer))
                        {
                            var backgroundColor = tagComparer.GetTagColor(tag) ?? SystemColors.Info;
                            var textColor = ColorService.ContrastColor(backgroundColor);
                            var size = g.MeasureString(tag, this.Font);
                            using (var backgroundBrush = new SolidBrush(backgroundColor))
                            using (var textBrush = new SolidBrush(textColor))
                            {
                                var topLeft = new PointF(bounds.Left + xOffset, bounds.Top + padding);
                                g.FillRectangle(backgroundBrush, new RectangleF(topLeft, size));
                                g.DrawString(tag, this.Font, textBrush, topLeft);
                            }

                            // TODO: Break on out-of-bounds.
                            xOffset += size.Width + padding;
                        }
                    }
                },
                {
                    Column.FileSize,
                    HorizontalAlignment.Right,
                    r => r.FileSize,
                    value => ByteSize.FromBytes(value).ToString(),
                    (a, b) => a.FileSize.CompareTo(b.FileSize)
                },
                {
                    Column.Rating,
                    HorizontalAlignment.Right,
                    r => r.Rating,
                    value => value != null ? $"{Math.Round(value.Value)}{(value.Count < 15 ? "?" : string.Empty)}" : string.Empty,
                    (a, b) => Rating.Compare(a.Rating, b.Rating)
                },
            }.ToImmutableDictionary(c => c.Column);

            this.VirtualListDataSource = new DataSource(this, this.columnDefinitions);

            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;
            this.index.HashPersonAdded += this.Index_HashPersonAdded;
            this.index.HashPersonRemoved += this.Index_HashPersonRemoved;
            this.index.RatingUpdated += this.Index_RatingUpdated;
            this.index.TagRulesUpdated += this.Index_TagRulesUpdated;

            foreach (var column in this.columnDefinitions.Values.OrderBy(c => c.Index))
            {
                var columnHeader = this.columns[column.Column] = new OLVColumn();
                columnHeader.Name = column.Column.ToString();
                columnHeader.Text = column.Name;
                columnHeader.TextAlign = column.HorizontalAlignment;
                columnHeader.AspectGetter = row => row == null ? null : column.GetValue((SearchResult)row);
                columnHeader.AspectToStringConverter = value => value == null ? null : column.FormatValue(value);

                if (column.DrawSubItem != null)
                {
                    columnHeader.Renderer = new ColumnRenderer(column.DrawSubItem)
                    {
                        ListView = this,
                    };
                }

                if (column.GetImage != null)
                {
                    columnHeader.ImageGetter = row => row == null ? null : column.GetImage((SearchResult)row);
                }

                this.Columns.Add(columnHeader);
            }

            this.ColumnWidthChanged += this.Internal_ColumnWidthChanged;
            this.BeforeSorting += this.Internal_BeforeSorting;
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
                this.orderdResults.Clear();
                this.orderdResults.AddRange(value);
                this.SetObjects(this.orderdResults);
            }
        }

        public IList<SearchResult> SelectedResults =>
            this.SelectedObjects.Cast<SearchResult>().ToList();

        public string SortColumn
        {
            get => this.PrimarySortColumn?.Name;
            set => this.PrimarySortColumn = this.AllColumns.Where(c => c.Name == value).FirstOrDefault();
        }

        public bool SortDescending
        {
            get => this.PrimarySortOrder == SortOrder.Descending;
            set => this.PrimarySortOrder = value ? SortOrder.Descending : SortOrder.Ascending;
        }

        private TagComparer TagComparer => this.tagComparer ?? (this.tagComparer = this.index.TagEngine.GetTagComparer());

        /// <inheritdoc/>
        public override void SetObjects(IEnumerable collection, bool preserveState)
        {
            var before = new HashSet<int>(this.SelectedIndices.Cast<int>());
            this.selectionChangedInvoked = false;
            base.SetObjects(collection, preserveState);
            if (!before.SetEquals(this.SelectedIndices.Cast<int>()) && !this.selectionChangedInvoked)
            {
                // TODO: this.TriggerDeferredSelectionChangedEvent(); after v2.9.1
                this.OnSelectionChanged(EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        protected override void OnSelectionChanged(EventArgs e)
        {
            this.selectionChangedInvoked = true;
            base.OnSelectionChanged(e);
        }

        private static string GetBestPath(SearchResult searchResult) => searchResult == null ? null : searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

        private static string GetImageKey(SearchResult r)
        {
            switch (r.FileType)
            {
                case string type when FileTypeHelper.IsAudio(type):
                    return "audio-file";

                case "image/gif":
                    return "video-file";

                case string type when FileTypeHelper.IsImage(type):
                    return "image-file";

                case string type when FileTypeHelper.IsVideo(type):
                    var sizes = new List<double>(2);
                    if (r.Details.TryGetValue("Width", out var widthObj))
                    {
                        sizes.Add(Convert.ToDouble(widthObj, CultureInfo.InvariantCulture));
                    }

                    if (r.Details.TryGetValue("Height", out var heightObj))
                    {
                        sizes.Add(Convert.ToDouble(heightObj, CultureInfo.InvariantCulture));
                    }

                    var extents = sizes.Aggregate(
                        new { Min = default(double?), Max = default(double?) },
                        (a, v) => new
                        {
                            Min = a.Min < v ? a.Min : v,
                            Max = a.Max > v ? a.Max : v,
                        });

                    if (extents.Max > 7680)
                    {
                        return "modern-tv-uhd";
                    }
                    else if (extents.Max > 4096)
                    {
                        return "modern-tv-8k";
                    }
                    else if (extents.Max >= 3840)
                    {
                        return "modern-tv-4k";
                    }
                    else if (extents.Min >= 1080)
                    {
                        return "modern-tv-hd";
                    }

                    return "modern-tv-flat";

                default: return "common-file";
            }
        }

        private int GetImageIndex(string key) =>
            (this.View == View.LargeIcon ? this.LargeImageList : this.SmallImageList)?.Images?.IndexOfKey(key) ?? -1;

        private void Index_HashPersonAdded(object sender, ItemAddedEventArgs<(HashPerson hash, Person person)> e)
        {
            this.UpdateSearchResult(e.Item.hash.Hash);
        }

        private void Index_HashPersonRemoved(object sender, ItemRemovedEventArgs<HashPerson> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void Index_RatingUpdated(object sender, ItemUpdatedEventArgs<Rating> e)
        {
            this.UpdateSearchResult(e.Item.Hash);
        }

        private void Index_TagRulesUpdated(object sender, ItemUpdatedEventArgs<TagRuleEngine> e)
        {
            this.tagComparer = null;
        }

        private void Internal_BeforeSorting(object sender, BeforeSortingEventArgs e)
        {
            if (this.LastSortColumn != e.ColumnToSort)
            {
                e.SecondaryColumnToSort = this.SecondarySortColumn = this.LastSortColumn;
                e.SecondarySortOrder = this.SecondarySortOrder = this.LastSortOrder;
            }
        }

        private void Internal_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            this.columnsSized = true;
        }

        private void UpdateSearchResult(string hash)
        {
            var searchResult = this.orderdResults.SingleOrDefault(r => r.Hash == hash);
            if (searchResult != null)
            {
                this.InvokeIfRequired(() =>
                {
                    this.RefreshObject(searchResult);
                });
            }
        }

        private class ColumnDefinition<T> : ColumnDefinition
        {
            public ColumnDefinition(
                Column column,
                HorizontalAlignment horizontalAlignment,
                Func<SearchResult, T> getValue,
                Func<T, string> formatValue,
                Comparison<SearchResult> comparison = null,
                Func<SearchResult, object> getImage = null,
                Action<Graphics, Rectangle, SearchResult> drawSubItem = null)
                : base(column, horizontalAlignment, r => getValue(r), value => formatValue((T)value), comparison, getImage, drawSubItem)
            {
                this.GetValue = getValue;
                this.FormatValue = formatValue;
            }

            public new Func<T, string> FormatValue { get; }

            public new Func<SearchResult, T> GetValue { get; }
        }

        private class ColumnDefinition
        {
            public ColumnDefinition(
                Column column,
                HorizontalAlignment horizontalAlignment,
                Func<SearchResult, object> getValue,
                Func<object, string> formatValue,
                Comparison<SearchResult> comparison = null,
                Func<SearchResult, object> getImage = null,
                Action<Graphics, Rectangle, SearchResult> drawSubItem = null)
            {
                this.Column = column;
                this.HorizontalAlignment = horizontalAlignment;
                this.Name = Resources.ResourceManager.GetString($"{column}Column", CultureInfo.CurrentCulture);
                this.Index = (int)column;
                this.GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
                this.FormatValue = formatValue ?? throw new ArgumentNullException(nameof(formatValue));
                this.Comparison = comparison ?? ((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(this.GetValue(a), this.GetValue(b)));
                this.GetImage = getImage;
                this.DrawSubItem = drawSubItem;
            }

            public Column Column { get; }

            public Comparison<SearchResult> Comparison { get; }

            public Action<Graphics, Rectangle, SearchResult> DrawSubItem { get; }

            public Func<object, string> FormatValue { get; }

            public Func<SearchResult, object> GetImage { get; }

            public Func<SearchResult, object> GetValue { get; }

            public HorizontalAlignment HorizontalAlignment { get; }

            public int Index { get; }

            public string Name { get; }
        }

        private class ColumnDefinitionList : List<ColumnDefinition>
        {
            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue);

            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue, comparison);

            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue, Action<Graphics, Rectangle, SearchResult> drawSubItem) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue, drawSubItem);

            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison, Action<Graphics, Rectangle, SearchResult> drawSubItem) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue, comparison, drawSubItem);

            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue, Func<SearchResult, object> getImage) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue, getImage);

            public void Add<T>(Column column, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison, Func<SearchResult, object> getImage) =>
                this.Add(column, HorizontalAlignment.Left, getValue, formatValue, comparison, getImage);

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue));

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue, comparison));

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue, Action<Graphics, Rectangle, SearchResult> drawSubItem) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue, drawSubItem: drawSubItem));

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison, Action<Graphics, Rectangle, SearchResult> drawSubItem) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue, comparison, drawSubItem: drawSubItem));

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue, Func<SearchResult, object> getImage) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue, getImage: getImage));

            public void Add<T>(Column column, HorizontalAlignment horizontalAlignment, Func<SearchResult, T> getValue, Func<T, string> formatValue, Comparison<SearchResult> comparison, Func<SearchResult, object> getImage) =>
                this.Add(new ColumnDefinition<T>(column, horizontalAlignment, getValue, formatValue, comparison, getImage));
        }

        private class ColumnRenderer : BaseRenderer
        {
            private Action<Graphics, Rectangle, SearchResult> drawSubItem;

            public ColumnRenderer(Action<Graphics, Rectangle, SearchResult> drawSubItem)
            {
                this.drawSubItem = drawSubItem;
            }

            public override void Render(Graphics g, Rectangle r)
            {
                this.DrawBackground(g, r);
                var searchResult = this.RowObject as SearchResult;
                this.drawSubItem(g, r, searchResult);
            }
        }

        private class DataSource : FastObjectListDataSource
        {
            private ImmutableDictionary<Column, ColumnDefinition> columnDefinitions;

            public DataSource(FastObjectListView listView, ImmutableDictionary<Column, ColumnDefinition> columnDefinitions)
                : base(listView)
            {
                this.columnDefinitions = columnDefinitions;
            }

            public override void Sort(OLVColumn column, SortOrder sortOrder)
            {
                if (sortOrder != SortOrder.None)
                {
                    var orderBuilder = ImmutableList.CreateBuilder<(Column column, bool descending)>();
                    orderBuilder.Add(((Column)column.Index, sortOrder == SortOrder.Descending));
                    if (this.listView.SecondarySortColumn != null)
                    {
                        orderBuilder.Add(((Column)this.listView.SecondarySortColumn.Index, this.listView.SecondarySortOrder == SortOrder.Descending));
                    }

                    var comparer = new SearchResultsComparer(this.columnDefinitions)
                    {
                        SortOrder = orderBuilder.ToImmutable(),
                    };

                    this.ObjectList.Sort(comparer);
                    this.FilteredObjectList.Sort(comparer);
                }

                this.RebuildIndexMap();
            }
        }

        private class SearchResultsComparer : IComparer<SearchResult>, IComparer
        {
            private ImmutableDictionary<Column, ColumnDefinition> columnDefinitions;

            public SearchResultsComparer(ImmutableDictionary<Column, ColumnDefinition> columnDefinitions)
            {
                this.columnDefinitions = columnDefinitions;
            }

            public ImmutableList<(Column column, bool descending)> SortOrder { get; set; }

            public int Compare(SearchResult a, SearchResult b)
            {
                foreach (var (sortColumn, sortDescending) in this.SortOrder)
                {
                    var column = this.columnDefinitions[sortColumn];
                    var value = column.Comparison(a, b);

                    if (value != 0)
                    {
                        return !sortDescending
                            ? value
                            : (value > 0 ? -1 : 1);
                    }
                }

                return 0;
            }

            public int Compare(object a, object b) => this.Compare((SearchResult)a, (SearchResult)b);
        }
    }
}
