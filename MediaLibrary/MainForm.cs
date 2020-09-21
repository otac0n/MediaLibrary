// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ByteSizeLib;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class MainForm : Form
    {
        private const int FileSizeColumnIndex = 4;
        private const int NameColumnIndex = 0;
        private const int PathColumnIndex = 1;
        private const int PeopleColumnIndex = 2;
        private const int TagsColumnIndex = 3;

        private readonly MediaIndex index;
        private readonly Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        private readonly List<InProgressTask> tasks = new List<InProgressTask>();
        private bool columnsAutoSized = false;
        private double lastProgress;
        private int searchVersion;
        private MainFormListSorter sorter;
        private int taskVersion;

        public MainForm(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.InitializeComponent();
            this.nameColumn.Name = "Name";
            this.pathColumn.Name = "Path";
            this.peopleColumn.Name = "People";
            this.tagsColumn.Name = "Tags";
            this.fileSizeColumn.Name = "FileSize";
            this.listView.ListViewItemSorter = this.sorter = new MainFormListSorter();
            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;
            this.index.HashPersonAdded += this.Index_HashPersonAdded;
            this.index.HashPersonRemoved += this.Index_HashPersonRemoved;
            this.ApplySettings();
        }

        private static bool CanDrop(DragEventArgs e) =>
            e.AllowedEffect.HasFlag(DragDropEffects.Link) &&
            e.Data.GetDataPresent(DataFormats.FileDrop) &&
            ((string[])e.Data.GetData(DataFormats.FileDrop)).All(Directory.Exists);

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

        private static void UpdateListItem(ListViewItem item, SearchResult searchResult)
        {
            item.Tag = searchResult;
            UpdateListItemPath(item, searchResult);
            UpdateListItemPeople(item, searchResult);
            UpdateListItemTags(item, searchResult);
        }

        private static void UpdateListItemPath(ListViewItem item, SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.FirstOrDefault();
            item.SubItems[PathColumnIndex].Text = firstPath != null ? Path.GetDirectoryName(firstPath) : string.Empty;
            item.SubItems[PathColumnIndex].Tag = firstPath;
            item.SubItems[NameColumnIndex].Text = firstPath != null ? Path.GetFileNameWithoutExtension(firstPath) : searchResult.Hash;
            item.SubItems[NameColumnIndex].Tag = firstPath;
        }

        private static void UpdateListItemPeople(ListViewItem item, SearchResult searchResult)
        {
            item.SubItems[PeopleColumnIndex].Text = string.Join("; ", searchResult.People.Select(p => p.Name));
            item.SubItems[PeopleColumnIndex].Tag = searchResult.People;
        }

        private static void UpdateListItemTags(ListViewItem item, SearchResult searchResult)
        {
            item.SubItems[TagsColumnIndex].Text = string.Join("; ", searchResult.Tags);
            item.SubItems[TagsColumnIndex].Tag = searchResult.Tags;
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void AddIndexedFolderToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            using (var addIndexedPathForm = new AddIndexedPathForm(this.index))
            {
                if (addIndexedPathForm.ShowDialog(this) == DialogResult.OK)
                {
                    this.AddIndexedPath(addIndexedPathForm.SelectedPath);
                }
            }
        }

        private void AddIndexedPath(string selectedPath)
        {
            this.TrackTaskProgress(progress => this.index.AddIndexedPath(selectedPath, progress));
        }

        private void AddPeopleMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = (((ToolStripMenuItem)sender).Tag as IList<SearchResult>) ?? this.GetSelectedSearchResults();
            if (searchResults.Count > 0)
            {
                using (var addPeopleForm = new AddPeopleForm(this.index, searchResults))
                {
                    addPeopleForm.ShowDialog(this);
                }
            }
        }

        private void AddSavedSearchMenuItem(SavedSearch savedSearch)
        {
            var searchItem = new ToolStripMenuItem
            {
                Text = savedSearch.Name,
                Tag = savedSearch,
            };

            var editItem = new ToolStripMenuItem
            {
                Text = "Edit...",
            };

            var deleteItem = new ToolStripMenuItem
            {
                Text = "Delete...",
            };

            searchItem.Click += this.SavedSearchMenuItem_Click;

            editItem.Click += async (sender, args) =>
            {
                using (var editSavedSearchForm = new EditSavedSearchForm(savedSearch))
                {
                    if (editSavedSearchForm.ShowDialog(this) == DialogResult.OK)
                    {
                        savedSearch = editSavedSearchForm.SavedSearch;
                        await this.index.UpdateSavedSearch(savedSearch).ConfigureAwait(true);
                        searchItem.Text = savedSearch.Name;
                        searchItem.Tag = savedSearch;
                    }
                }
            };

            deleteItem.Click += async (sender, args) =>
            {
                var result = MessageBox.Show($"This will delete the saved search {savedSearch.Name} (ID: {savedSearch.SearchId}). This is a destructive operation and cannot be undone. Are you sure you want to delete this search?", "Are you sure?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    await this.index.RemoveSavedSearch(savedSearch).ConfigureAwait(true);
                    this.savedSearchesMenuItem.DropDownItems.Remove(searchItem);
                }
            };

            searchItem.DropDownItems.AddRange(new[] { editItem, deleteItem });

            this.savedSearchesSeparator.Visible = true;
            this.savedSearchesMenuItem.DropDownItems.Add(searchItem);
        }

        private void AddTagsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = (((ToolStripMenuItem)sender).Tag as IList<SearchResult>) ?? this.GetSelectedSearchResults();
            if (searchResults.Count > 0)
            {
                using (var editTagsForm = new EditTagsForm(this.index, searchResults))
                {
                    editTagsForm.ShowDialog(this);
                }
            }
        }

        private void ApplySettings()
        {
            var settings = Properties.Settings.Default;
            void Save()
            {
                // TODO: Throttle.
                settings.Save();
            }

            this.showPreviewMenuItem.Checked = settings.ShowPreview;
            this.showPreviewMenuItem.CheckedChanged += (sender, args) =>
            {
                settings.ShowPreview = this.showPreviewMenuItem.Checked;
                Save();
            };

            var columnLookup = this.listView.Columns.Cast<ColumnHeader>().ToDictionary(c => c.Name);
            var columnsDesc = settings.Columns.Split(",; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (columnLookup.TryGetValue(settings.SortColumn, out var sortColumnHeader))
            {
                this.sorter.SortColumn = sortColumnHeader.Index;
                this.sorter.Descending = settings.SortDescending;
            }

            this.listView.BeginUpdate();

            var displayIndex = 0;
            foreach (var desc in columnsDesc)
            {
                var parts = desc.Split(new[] { ':' }, 2);
                if (columnLookup.TryGetValue(parts[0], out var columnHeader))
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

            this.listView.EndUpdate();

            void ColumnsChanged(IEnumerable<ColumnHeader> headers)
            {
                settings.Columns = string.Join(",", headers.Select(h => h.Name + (this.columnsAutoSized ? $":{h.Width}" : string.Empty)));
                Save();
            }

            var columnHeadersInOrder = this.listView.Columns.Cast<ColumnHeader>().OrderBy(c => c.DisplayIndex);
            this.listView.ColumnWidthChanged += (sender, args) => ColumnsChanged(columnHeadersInOrder);
            this.listView.ColumnReordered += (sender, args) =>
            {
                var list = columnHeadersInOrder.ToList();
                var item = list[args.OldDisplayIndex];
                list.RemoveAt(args.OldDisplayIndex);
                list.Insert(args.NewDisplayIndex, item);
                ColumnsChanged(list);
            };
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            // HACK: Exclude one text box, to avoid clobbering the clipboard.
            if (this.searchBox.Focused)
            {
                return;
            }

            var paths = this.GetSelectedPaths().ToArray();
            if (paths.Length > 0)
            {
                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.FileDrop, autoConvert: true, paths);
                dataObject.SetText(string.Join(Environment.NewLine, paths));
                Clipboard.SetDataObject(dataObject, copy: true);
            }
        }

        private ListViewItem CreateListItem(SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();

            var columns = new ListViewItem.ListViewSubItem[5];

            columns[NameColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = searchResult.Paths.Count > 0 ? Path.GetFileNameWithoutExtension(firstPath) : searchResult.Hash,
                Tag = firstPath,
            };

            columns[PathColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = searchResult.Paths.Count > 0 ? Path.GetDirectoryName(firstPath) : string.Empty,
                Tag = firstPath,
            };

            columns[TagsColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = string.Join("; ", searchResult.Tags),
                Tag = searchResult.Tags,
            };
            columns[FileSizeColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = ByteSize.FromBytes(searchResult.FileSize).ToString(),
                Tag = searchResult.FileSize,
            };
            columns[PeopleColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = string.Join("; ", searchResult.People.Select(p => p.Name)),
                Tag = searchResult.People,
            };

            return this.items[searchResult.Hash] = new ListViewItem(columns, GetImageKey(searchResult.FileType))
            {
                Tag = searchResult,
            };
        }

        private void DetailsMenuItem_Click(object sender, EventArgs e)
        {
            this.thumbnailsMenuItem.Checked = !this.detailsMenuItem.Checked;

            if (this.detailsMenuItem.Checked)
            {
                this.listView.View = View.Details;
            }
        }

        private void EditPeopleMenuItem_Click(object sender, EventArgs e)
        {
            using (var editPeopleForm = new EditPeopleForm(this.index))
            {
                editPeopleForm.ShowDialog(this);
            }
        }

        private void EditTagRulesMenuItem_Click(object sender, EventArgs e)
        {
            using (var editTagRulesForm = new EditTagRulesForm(this.index))
            {
                editTagRulesForm.ShowDialog(this);
            }
        }

        private void EditToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            var searchResults = this.GetSelectedSearchResults();
            this.favoriteMainMenuItem.CheckState = searchResults.All(r => r.Tags.Contains("favorite")) ? CheckState.Checked : CheckState.Unchecked;
        }

        private async void FavoriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var senderMenu = (ToolStripMenuItem)sender;

            bool @checked;
            if (!(senderMenu.Tag is IList<SearchResult> searchResults))
            {
                searchResults = this.GetSelectedSearchResults();
                @checked = searchResults.All(r => r.Tags.Contains("favorite"));
            }
            else
            {
                @checked = senderMenu.CheckState == CheckState.Checked;
            }

            if (searchResults.Count > 0)
            {
                if (@checked)
                {
                    foreach (var searchResult in searchResults)
                    {
                        await this.index.RemoveHashTag(new HashTag(searchResult.Hash, "favorite")).ConfigureAwait(false);
                    }
                }
                else
                {
                    foreach (var searchResult in searchResults)
                    {
                        await this.index.AddHashTag(new HashTag(searchResult.Hash, "favorite")).ConfigureAwait(false);
                    }
                }
            }
        }

        private void FindDuplicatesMenuItem_Click(object sender, EventArgs e)
        {
            using (var findDuplicatesForm = new FindDuplicatesForm(this.index))
            {
                findDuplicatesForm.ShowDialog(this);
            }
        }

        private IEnumerable<string> GetSelectedPaths()
        {
            var items = this.listView.SelectedItems.Cast<ListViewItem>().ToList();
            foreach (var item in items)
            {
                foreach (var path in ((SearchResult)item.Tag).Paths)
                {
                    if (File.Exists(PathEncoder.ExtendPath(path)))
                    {
                        yield return path;
                        break;
                    }
                }
            }
        }

        private List<SearchResult> GetSelectedSearchResults() => this.listView.SelectedItems.Cast<ListViewItem>().Select(i => (SearchResult)i.Tag).ToList();

        private List<SearchResult> GetVisibleSearchResults() => this.listView.Items.Cast<ListViewItem>().Select(i => (SearchResult)i.Tag).ToList();

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

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var column = this.listView.Columns[e.Column];
            if (this.sorter.SortColumn != column.Index)
            {
                this.sorter.SortColumn = column.Index;
                this.sorter.Descending = false;
            }
            else
            {
                this.sorter.Descending = !this.sorter.Descending;
            }

            // TODO: Raise sort changed event to notify settings to update.
            this.listView.Sort();
        }

        private async void ListView_DoubleClick(object sender, MouseEventArgs e)
        {
            if (this.listView.HitTest(e.X, e.Y).Item != null)
            {
                this.OpenMenuItem_Click(this.openContextMenuItem, e);
            }
        }

        private void ListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            this.UpdatePreview();
        }

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                var item = this.listView.FocusedItem;
                if (item.Bounds.Contains(e.Location))
                {
                    var searchResults = this.GetSelectedSearchResults();
                    this.favoriteContextMenuItem.CheckState = searchResults.All(r => r.Tags.Contains("favorite")) ? CheckState.Checked : CheckState.Unchecked;
                    this.favoriteContextMenuItem.Tag = searchResults;
                    this.editTagsContextMenuItem.Tag = searchResults;
                    this.addPeopleContextMenuItem.Tag = searchResults;
                    this.itemContextMenu.Show(Cursor.Position);
                }
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.UpdatePreview();
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (CanDrop(e))
            {
                foreach (var dir in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    this.AddIndexedPath(dir);
                }
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = CanDrop(e)
                ? DragDropEffects.Link
                : DragDropEffects.None;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            this.savedSearchesMenuItem.Enabled = false;
            await this.index.Initialize().ConfigureAwait(true);

            foreach (var savedSearch in await this.index.GetAllSavedSearches().ConfigureAwait(true))
            {
                this.AddSavedSearchMenuItem(savedSearch);
            }

            this.savedSearchesMenuItem.Enabled = true;

            this.TrackTaskProgress(async progress =>
            {
                await this.index.Rescan(progress).ConfigureAwait(true);
            });
        }

        private void MergePeopleMenuItem_Click(object sender, EventArgs e)
        {
            using (var mergePeopleForm = new MergePeopleForm(this.index))
            {
                mergePeopleForm.ShowDialog(this);
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var path in this.GetSelectedPaths())
            {
                Process.Start(path);
            }
        }

        private void OpenSlideshow(bool shuffle = false, bool autoPlay = false)
        {
            var searchResults = this.listView.SelectedItems.Count > 1
                ? this.GetSelectedSearchResults()
                : this.GetVisibleSearchResults();
            new SlideShowForm(this.index, searchResults, shuffle, autoPlay).Show(this);
        }

        private void PlayAllButton_Click(object sender, EventArgs e)
        {
            this.OpenSlideshow(autoPlay: true);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            this.OpenSlideshow();
        }

        private void SavedSearchMenuItem_Click(object sender, EventArgs e)
        {
            var savedSearch = (SavedSearch)((ToolStripMenuItem)sender).Tag;
            this.searchBox.Text = savedSearch.Query;
            this.viewButton.HideDropDown();
        }

        private async void SaveThisSearchMenuItem_Click(object sender, EventArgs e)
        {
            var searchText = this.searchBox.Text;
            using (var saveSearchForm = new SaveSearchForm())
            {
                if (saveSearchForm.ShowDialog(this) == DialogResult.OK)
                {
                    var savedSearch = await this.index.AddSavedSearch(saveSearchForm.SelectedName, searchText).ConfigureAwait(true);
                    this.AddSavedSearchMenuItem(savedSearch);
                }
            }
        }

        private void SearchBookmark_Click(object sender, EventArgs e)
        {
            string tag = null;
            if (sender is Control control)
            {
                tag = control.Tag as string;
            }
            else if (sender is ToolStripItem toolStripItem)
            {
                tag = toolStripItem.Tag as string;
            }

            this.searchBox.Text = tag ?? string.Empty;
        }

        private async void SearchBox_TextChangedAsync(object sender, EventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref this.searchVersion);
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(true);
            if (this.searchVersion != searchVersion)
            {
                return;
            }

            IList<SearchResult> data;
            try
            {
                data = await this.index.SearchIndex(this.searchBox.Text).ConfigureAwait(true);
            }
            catch
            {
                data = Array.Empty<SearchResult>();
            }

            if (this.searchVersion == searchVersion)
            {
                var newHashes = new HashSet<string>(data.Select(i => i.Hash));

                this.listView.BeginUpdate();
                this.listView.ListViewItemSorter = null;

                for (var i = this.listView.Items.Count - 1; i >= 0; i--)
                {
                    var item = this.listView.Items[i];
                    var hash = ((SearchResult)item.Tag).Hash;
                    if (!newHashes.Contains(hash))
                    {
                        this.items.Remove(hash);
                        this.listView.Items.RemoveAt(i);
                    }
                }

                foreach (var item in data)
                {
                    if (!this.items.TryGetValue(item.Hash, out var _))
                    {
                        this.listView.Items.Add(this.CreateListItem(item));
                    }
                }

                if (!this.columnsAutoSized && data.Count > 0)
                {
                    this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    this.columnsAutoSized = true;
                }

                this.listView.ListViewItemSorter = this.sorter;
                this.listView.Sort();

                this.listView.EndUpdate();
            }
        }

        private void ShowPreviewMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            this.splitContainer.Panel2Collapsed = !(this.preview.Visible = this.showPreviewMenuItem.Checked);
        }

        private void ShuffleAllButton_Click(object sender, EventArgs e)
        {
            this.OpenSlideshow(shuffle: true, autoPlay: true);
        }

        private void ThumbnailsMenuItem_Click(object sender, EventArgs e)
        {
            this.detailsMenuItem.Checked = !this.thumbnailsMenuItem.Checked;

            if (this.thumbnailsMenuItem.Checked)
            {
                this.listView.View = View.LargeIcon;
            }
        }

        private void TrackTaskProgress(Func<IProgress<RescanProgress>, Task> getTask)
        {
            var task = new InProgressTask(getTask, this.UpdateProgress);

            lock (this.tasks)
            {
                this.lastProgress = 0;
                this.tasks.Add(task);
                this.taskVersion++;
            }

            this.UpdateProgress();

            task.Task.ContinueWith(
                _ =>
                {
                    lock (this.tasks)
                    {
                        this.tasks.Remove(task);
                        this.taskVersion++;
                    }

                    this.UpdateProgress();
                },
                TaskScheduler.Current);
        }

        private void UpdatePreview()
        {
            this.preview.PreviewItems = this.listView.SelectedItems.Cast<ListViewItem>().Select(i => (SearchResult)i.Tag).ToList();
        }

        private void UpdateProgress()
        {
            RescanProgress progress;
            int taskVersion;
            lock (this.tasks)
            {
                taskVersion = this.taskVersion;
                progress = RescanProgress.Aggregate(ref this.lastProgress, this.tasks.Select(t => t.Progress).ToArray());
            }

            this.InvokeIfRequired(() =>
            {
                lock (this.tasks)
                {
                    if (this.taskVersion != taskVersion)
                    {
                        return;
                    }

                    if (this.tasks.Count == 0)
                    {
                        this.mainProgressBar.Visible = false;
                    }
                    else
                    {
                        this.mainProgressBar.Visible = true;
                        this.mainProgressBar.Value = (int)Math.Floor(progress.Estimate * this.mainProgressBar.Maximum);
                        this.mainProgressBar.ToolTipText = $"{progress.Estimate:P0} ({progress.PathsProcessed}/{progress.PathsDiscovered}{(progress.DiscoveryComplete ? string.Empty : "?")})";
                    }
                }
            });
        }

        private void UpdateSearchResult(string hash, Action<ListViewItem, SearchResult> updateListViewItem)
        {
            if (this.items.TryGetValue(hash, out var item))
            {
                var searchResult = (SearchResult)item.Tag;
                this.InvokeIfRequired(() => updateListViewItem(item, searchResult));
            }
        }

        private class InProgressTask
        {
            public InProgressTask(Func<IProgress<RescanProgress>, Task> getTask, Action updateProgress)
            {
                this.Progress = new RescanProgress(0, 0, 0, false);
                this.Task = getTask(OnProgress.Do<RescanProgress>(progress =>
                {
                    this.Progress = progress;
                    updateProgress();
                }));
            }

            public RescanProgress Progress { get; private set; }

            public Task Task { get; }
        }

        private class MainFormListSorter : IComparer<ListViewItem>, IComparer
        {
            public bool Descending { get; set; }

            public int SortColumn { get; set; }

            public int Compare(ListViewItem a, ListViewItem b)
            {
                int value;
                switch (this.SortColumn)
                {
                    case NameColumnIndex:
                        value = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[NameColumnIndex].Text, b.SubItems[NameColumnIndex].Text);
                        break;

                    case PathColumnIndex:
                        value = PathComparer.Instance.Compare((string)a.SubItems[NameColumnIndex].Tag, (string)b.SubItems[NameColumnIndex].Tag);
                        break;

                    case TagsColumnIndex:
                        value = ((ImmutableHashSet<string>)a.SubItems[TagsColumnIndex].Tag).Count.CompareTo(((ImmutableHashSet<string>)b.SubItems[TagsColumnIndex].Tag).Count);
                        break;

                    case FileSizeColumnIndex:
                        value = ((long)a.SubItems[FileSizeColumnIndex].Tag).CompareTo((long)b.SubItems[FileSizeColumnIndex].Tag);
                        break;

                    case PeopleColumnIndex:
                        value = ((ImmutableHashSet<Person>)a.SubItems[PeopleColumnIndex].Tag).Count.CompareTo(((ImmutableHashSet<Person>)b.SubItems[PeopleColumnIndex].Tag).Count);
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
