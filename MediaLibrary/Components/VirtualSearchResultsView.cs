// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;
    using BrightIdeasSoftware;
    using ByteSizeLib;
    using MediaLibrary.Properties;
    using MediaLibrary.Services;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search;
    using TaggingLibrary;

    public class VirtualSearchResultsView : FastObjectListView
    {
        private readonly ImmutableDictionary<Column, ColumnDefinition> columnDefinitions;
        private readonly Dictionary<Column, OLVColumn> columns = new Dictionary<Column, OLVColumn>();
        private readonly MediaIndex index;
        private readonly List<SearchResult> orderdResults = new List<SearchResult>();
        private bool columnsSized = false;
        private PersonComparer personComparer;
        private bool suppressSelectionChanged;
        private TagComparer tagComparer;

        public VirtualSearchResultsView(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.AllowColumnReorder = true;
            this.FullRowSelect = true;
            this.CanUseApplicationIdle = false;
            this.View = View.Details;

            this.columnDefinitions = new List<ColumnDefinition>
            {
                ColumnDefinition.Create(
                    Column.Path,
                    HorizontalAlignment.Left,
                    r =>
                    {
                        var path = GetBestPath(r);
                        return path == null ? null : Path.GetDirectoryName(path);
                    },
                    value => value ?? string.Empty,
                    (a, b) => PathComparer.Instance.Compare(a, b)),
                ColumnDefinition.Create(
                    Column.Name,
                    HorizontalAlignment.Left,
                    r =>
                    {
                        var path = GetBestPath(r);
                        return path == null ? null : Path.GetFileNameWithoutExtension(path);
                    },
                    value => value ?? string.Empty,
                    (a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(GetBestPath(a), GetBestPath(b)),
                    getImage: r => GetImageKey(r)),
                ColumnDefinition.Create(
                    Column.People,
                    HorizontalAlignment.Left,
                    r => r.People,
                    value => string.Join("; ", value.Select(p => p.Name)),
                    (a, b) => this.PersonComparer.Compare(a.ToList(), b.ToList())),
                ColumnDefinition.Create(
                    Column.Tags,
                    HorizontalAlignment.Left,
                    r => r.Tags,
                    value => string.Join("; ", value),
                    (ISet<string> a, ISet<string> b) => this.TagComparer.Compare(a, b),
                    drawSubItem: (g, bounds, tags) =>
                    {
                        var tagComparer = this.TagComparer;
                        var baseSize = g.MeasureString("#", this.Font);
                        var padding = (int)Math.Floor((bounds.Height - baseSize.Height) / 2);

                        var xOffset = 0f;
                        if (tags.Contains(TagComparer.FavoriteTag))
                        {
                            var size = bounds.Height - padding * 2;
                            g.DrawImage(Resources.love_it_filled, new RectangleF(bounds.Left + xOffset, bounds.Top + padding, size, size));
                            xOffset += size + padding;
                        }

                        foreach (var tag in tags.Where(t => t != TagComparer.FavoriteTag).OrderBy(t => t, tagComparer))
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
                    }),
                ColumnDefinition.Create(
                    Column.FileSize,
                    HorizontalAlignment.Right,
                    r => r.FileSize,
                    value => ByteSize.FromBytes(value).ToString(),
                    (a, b) => a.CompareTo(b)),
                ColumnDefinition.Create(
                    Column.Duration,
                    HorizontalAlignment.Right,
                    r => GetDetails<TimeSpan?>(r, nameof(Column.Duration), value => TimeSpan.FromSeconds(Convert.ToDouble(value, CultureInfo.InvariantCulture))),
                    value => FormatTimeSpan(value),
                    Nullable.Compare),
                ColumnDefinition.Create(
                    Column.Rating,
                    HorizontalAlignment.Right,
                    r => r.Rating,
                    value => value != null ? $"{Math.Round(value.Value)}{(value.Count < 15 ? "?" : string.Empty)}" : string.Empty,
                    (a, b) => Rating.Compare(a.Rating, b.Rating)),
                ColumnDefinition.Create(
                    Column.VisualHash,
                    HorizontalAlignment.Left,
                    r => (ulong?)GetDetails<long?>(r, ImageDetailRecognizer.Properties.AverageIntensityHash, value => Convert.ToInt64(value, CultureInfo.InvariantCulture)),
                    value => value != null ? $"0x{value:X16}" : string.Empty,
                    Nullable.Compare,
                    true,
                    drawSubItem: (g, bounds, r) =>
                    {
                        if (r == null)
                        {
                            return;
                        }

                        var value = r.Value;

                        const int Margin = 2; // Margin of 1px, plus an additional 1px beyond the left beyond to compensate for the system carat. This could have trouble if the selection rectangle is too fat (wider than 1px), but we assume a 1px border for now.
                        const int HashEdgeSize = 8;

                        var left = Margin;
                        using (var bmp = new Bitmap(HashEdgeSize, HashEdgeSize))
                        {
                            var bmpData = bmp.LockBits(new Rectangle(0, 0, HashEdgeSize, HashEdgeSize), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

                            var colorData = new byte[1];
                            var scan = bmpData.Scan0;
                            for (var y = 1; y <= HashEdgeSize; y++, scan += bmpData.Stride)
                            {
                                colorData[0] = (byte)(value >> ((HashEdgeSize - y) * HashEdgeSize));
                                Marshal.Copy(colorData, 0, scan, 1);
                            }

                            bmp.UnlockBits(bmpData);

                            g.DrawImageUnscaled(bmp, bounds.X + left, bounds.Top + (bounds.Height - HashEdgeSize) / 2);
                            left += HashEdgeSize + Margin;
                        }

                        using (var textBrush = new SolidBrush(this.ForeColor))
                        using (var monspaced = new Font(FontFamily.GenericMonospace, this.Font.Size))
                        {
                            g.DrawString($"{value:x16}", monspaced, textBrush, bounds.X + left, bounds.Top + Margin);
                        }
                    }),
                ColumnDefinition.Create(
                    Column.Resolution,
                    HorizontalAlignment.Right,
                    r =>
                    {
                        var width = GetDetails<long?>(r, ImageDetailRecognizer.Properties.Width, value => Convert.ToInt64(value, CultureInfo.InvariantCulture));
                        var height = GetDetails<long?>(r, ImageDetailRecognizer.Properties.Height, value => Convert.ToInt64(value, CultureInfo.InvariantCulture));
                        return width != null && height != null ? (width.Value, height.Value) : default((long width, long height)?);
                    },
                    value =>
                    {
                        if (value == null)
                        {
                            return string.Empty;
                        }

                        var (width, height) = value.Value;
                        var byteSize = ByteSize.FromBytes(width * height);
                        var byteSizeValue = byteSize.LargestWholeNumberDecimalValue;
                        var byteSizeSymbol = byteSize.LargestWholeNumberDecimalSymbol.Replace('B', 'P');
                        return $"{byteSizeValue:0.0} {byteSizeSymbol} ({width}×{height})";
                    },
                    (a, b) => Nullable.Compare(a?.width * a?.height, b?.width * b?.height)),
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
                var columnHeader = new OLVColumn();
                columnHeader.Name = column.Column.ToString();
                columnHeader.Text = column.Name;
                columnHeader.TextAlign = column.HorizontalAlignment;
                columnHeader.Groupable = column.Groupable;
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

                this.Columns.Add(this.columns[column.Column] = columnHeader);
            }

            this.AlwaysGroupByColumn = this.columns[Column.VisualHash];
            this.ColumnWidthChanged += this.Internal_ColumnWidthChanged;
            this.BeforeSorting += this.Internal_BeforeSorting;
        }

        private enum Column
        {
            Path,
            Name,
            People,
            Tags,
            Rating,
            FileSize,
            Resolution,
            Duration,
            VisualHash,
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
            set => this.PrimarySortColumn = Enum.TryParse<Column>(value, out var column) && this.columns.TryGetValue(column, out var header) ? header : null;
        }

        public bool SortDescending
        {
            get => this.PrimarySortOrder == SortOrder.Descending;
            set => this.PrimarySortOrder = value ? SortOrder.Descending : SortOrder.Ascending;
        }

        private PersonComparer PersonComparer => this.personComparer ?? (this.personComparer = new PersonComparer());

        private TagComparer TagComparer => this.tagComparer ?? (this.tagComparer = this.index.TagEngine.GetTagComparer());

        /// <inheritdoc/>
        public override void SetObjects(IEnumerable collection, bool preserveState)
        {
            var before = this.SelectedObjects;

            if (before.Count > 0)
            {
                var intersection = new HashSet<SearchResult>(before.Cast<SearchResult>());
                intersection.IntersectWith(collection.Cast<SearchResult>());
                this.suppressSelectionChanged = before.Count == intersection.Count;
            }

            this.SelectedIndex = -1;
            base.SetObjects(collection, preserveState);
            this.SelectObjects(before);
        }

        /// <inheritdoc/>
        protected override void OnSelectionChanged(EventArgs e)
        {
            if (this.suppressSelectionChanged)
            {
                this.suppressSelectionChanged = false;
                return;
            }

            base.OnSelectionChanged(e);
        }

        private static string FormatTimeSpan(TimeSpan? value)
        {
            if (value is TimeSpan duration)
            {
                var formatted = new StringBuilder();
                if (duration.TotalDays >= 1)
                {
                    formatted.Append(duration.Days).Append("d");
                }

                if (duration.TotalHours >= 1)
                {
                    formatted.AppendFormat("{0:d2}", duration.Hours).Append("h");
                }

                if (duration.TotalMinutes >= 1)
                {
                    formatted.AppendFormat("{0:d2}", duration.Minutes).Append("m");
                }

                formatted.AppendFormat("{0:d2}", duration.Seconds).Append("s");

                return formatted.ToString();
            }

            return string.Empty;
        }

        private static string GetBestPath(SearchResult searchResult) =>
            searchResult == null ? null : searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

        private static T GetDetails<T>(SearchResult result, string name, Func<object, T> converter) =>
            result.Details.TryGetValue(name, out var value) && value is object ? converter(value) : default;

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
            public static readonly Comparison<T> DefaultComparison =
                typeof(T) == typeof(string)
                    ? new Comparison<T>((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a, b))
                    : Comparer<T>.Default.Compare;

            public ColumnDefinition(
                Column column,
                HorizontalAlignment horizontalAlignment,
                Func<SearchResult, T> getValue,
                Func<T, string> formatValue,
                Comparison<SearchResult> comparison,
                bool groupable,
                Func<SearchResult, object> getImage,
                Action<Graphics, Rectangle, SearchResult> drawSubItem)
                : base(
                    column,
                    horizontalAlignment,
                    r => getValue(r),
                    value => formatValue((T)value),
                    comparison,
                    groupable,
                    getImage,
                    drawSubItem)
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
                Comparison<SearchResult> comparison,
                bool groupable,
                Func<SearchResult, object> getImage,
                Action<Graphics, Rectangle, SearchResult> drawSubItem)
            {
                this.Column = column;
                this.HorizontalAlignment = horizontalAlignment;
                this.Name = Resources.ResourceManager.GetString($"{column}Column", CultureInfo.CurrentCulture);
                this.Index = (int)column;
                this.GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
                this.FormatValue = formatValue ?? throw new ArgumentNullException(nameof(formatValue));
                this.Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
                this.Groupable = groupable;
                this.GetImage = getImage;
                this.DrawSubItem = drawSubItem;
            }

            public Column Column { get; }

            public Comparison<SearchResult> Comparison { get; }

            public Action<Graphics, Rectangle, SearchResult> DrawSubItem { get; }

            public Func<object, string> FormatValue { get; }

            public Func<SearchResult, object> GetImage { get; }

            public Func<SearchResult, object> GetValue { get; }

            public bool Groupable { get; }

            public HorizontalAlignment HorizontalAlignment { get; }

            public int Index { get; }

            public string Name { get; }

            public static ColumnDefinition<T> Create<T>(
                Column column,
                HorizontalAlignment horizontalAlignment,
                Func<SearchResult, T> getValue,
                Func<T, string> formatValue,
                Comparison<T> comparison = null,
                bool groupable = false,
                Func<SearchResult, object> getImage = null,
                Action<Graphics, Rectangle, T> drawSubItem = null) =>
                Create(
                    column,
                    horizontalAlignment,
                    getValue,
                    formatValue,
                    comparison == null
                        ? new Comparison<SearchResult>((a, b) => ColumnDefinition<T>.DefaultComparison(getValue(a), getValue(b)))
                        : new Comparison<SearchResult>((a, b) => comparison(getValue(a), getValue(b))),
                    groupable,
                    getImage,
                    drawSubItem == null
                        ? default
                        : (g, r, t) => drawSubItem(g, r, getValue(t)));

            public static ColumnDefinition<T> Create<T>(
                Column column,
                HorizontalAlignment horizontalAlignment,
                Func<SearchResult, T> getValue,
                Func<T, string> formatValue,
                Comparison<SearchResult> comparison = null,
                bool groupable = false,
                Func<SearchResult, object> getImage = null,
                Action<Graphics, Rectangle, SearchResult> drawSubItem = null) =>
                new ColumnDefinition<T>(
                    column,
                    horizontalAlignment,
                    getValue,
                    formatValue,
                    comparison,
                    groupable,
                    getImage,
                    drawSubItem);
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
