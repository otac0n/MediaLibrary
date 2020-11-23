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

    public class VirtualSearchResultsView : FastObjectListView
    {
        private readonly ImmutableDictionary<Column, ColumnDefinition> columnDefinitions;
        private readonly Dictionary<Column, OLVColumn> columns = new Dictionary<Column, OLVColumn>();
        private readonly MediaIndex index;
        private readonly List<SearchResult> orderdResults = new List<SearchResult>();
        private bool columnsSized = false;

        public VirtualSearchResultsView(MediaIndex index)
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
                    r => GetImageKey(r.FileType)
                },
                {
                    Column.People,
                    r => r.People,
                    value => string.Join("; ", value.Select(p => p.Name)),
                    (a, b) => a.People.Count.CompareTo(b.People.Count)
                },
                {
                    Column.Tags,
                    r => r.Tags,
                    value => string.Join("; ", value),
                    (a, b) => a.Tags.Count.CompareTo(b.Tags.Count),
                    (g, bounds, r) =>
                    {
                        var engine = this.index.TagEngine;
                        var comparison = engine.GetTagComparison();
                        var baseSize = g.MeasureString("#", this.Font);
                        var padding = (int)Math.Floor((bounds.Height - baseSize.Height) / 2);

                        var xOffset = 0f;
                        foreach (var tag in r.Tags.OrderBy(t => t, Comparer<string>.Create(comparison)))
                        {
                            var backgroundColor = engine.GetTagColor(tag) ?? SystemColors.Info;
                            var textColor = ColorService.ContrastColor(backgroundColor);
                            var size = g.MeasureString(tag, this.Font);
                            using (var backgroundBrush = new SolidBrush(backgroundColor))
                            using (var textBrush = new SolidBrush(textColor))
                            {
                                var topLeft = new PointF(bounds.Left + xOffset, bounds.Top + padding);
                                g.FillRectangle(backgroundBrush, new RectangleF(topLeft, size));
                                g.DrawString(tag, this.Font, textBrush, topLeft);
                            }

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

            foreach (var column in this.columnDefinitions.Values.OrderBy(c => c.Index))
            {
                var columnHeader = this.columns[column.Column] = new OLVColumn();
                columnHeader.Name = column.Column.ToString();
                columnHeader.Text = column.Name;
                columnHeader.TextAlign = column.HorizontalAlignment;
                columnHeader.AspectGetter = row => column.GetValue((SearchResult)row);
                columnHeader.AspectToStringConverter = value => column.FormatValue(value);

                if (column.DrawSubItem != null)
                {
                    columnHeader.Renderer = new ColumnRenderer(column.DrawSubItem)
                    {
                        ListView = this,
                    };
                }

                if (column.GetImage != null)
                {
                    columnHeader.ImageGetter = row => column.GetImage((SearchResult)row);
                }

                this.Columns.Add(columnHeader);
            }

            this.ColumnWidthChanged += this.Internal_ColumnWidthChanged;
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
            get => this.PrimarySortColumn.Name;
            set => this.PrimarySortColumn = this.AllColumns.Where(c => c.Name == value).FirstOrDefault();
        }

        public bool SortDescending
        {
            get => this.PrimarySortOrder == SortOrder.Descending;
            set => this.PrimarySortOrder = value ? SortOrder.Descending : SortOrder.Ascending;
        }

        private static string GetBestPath(SearchResult searchResult) => searchResult == null ? null : searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

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
                    var comparer = new SearchResultsComparer(this.columnDefinitions)
                    {
                        SortColumn = (Column)column.Index,
                        Descending = sortOrder == SortOrder.Descending,
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

            public bool Descending { get; set; }

            public Column SortColumn { get; set; }

            public int Compare(SearchResult a, SearchResult b)
            {
                var column = this.columnDefinitions[this.SortColumn];
                var value = column.Comparison(a, b);

                return
                    !this.Descending || value == 0 ? value :
                    value > 0 ? -1 : 1;
            }

            public int Compare(object a, object b) => this.Compare((SearchResult)a, (SearchResult)b);
        }
    }
}
