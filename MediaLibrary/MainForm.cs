// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
        private const int PathColumnIndex = 0;
        private const int PeopleColumnIndex = 3;
        private const int SizeColumnIndex = 2;
        private const int TagsColumnIndex = 1;

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
            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;
            this.index.HashPersonAdded += this.Index_HashPersonAdded;
            this.index.HashPersonRemoved += this.Index_HashPersonRemoved;
            this.listView.ListViewItemSorter = this.sorter = new MainFormListSorter
            {
                SortColumn = PathColumnIndex,
            };
            this.TrackTaskProgress(async progress =>
            {
                await this.index.Initialize().ConfigureAwait(false);
                await index.Rescan(progress).ConfigureAwait(true);
            });
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

                case "image/bmp": return "image-bmp";
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
            var firstPath = searchResult.Paths.First();
            item.SubItems[PathColumnIndex].Text = searchResult.Paths.Count > 0 ? Path.GetFileNameWithoutExtension(firstPath) : searchResult.Hash;
            item.SubItems[PathColumnIndex].Tag = firstPath;
            item.SubItems[TagsColumnIndex].Text = string.Join(" ", searchResult.Tags);
            item.SubItems[TagsColumnIndex].Tag = searchResult.Tags;
            item.SubItems[PeopleColumnIndex].Text = string.Join("; ", searchResult.People.Select(p => p.Name));
            item.SubItems[PeopleColumnIndex].Tag = searchResult.People;
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
            var searchResults = (IList<SearchResult>)this.itemContextMenu.Tag;
            using (var addPeopleForm = new AddPeopleForm(this.index, searchResults))
            {
                addPeopleForm.ShowDialog(this);
            }
        }

        private void AddTagsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = (IList<SearchResult>)this.itemContextMenu.Tag;
            using (var editTagsForm = new EditTagsForm(this.index, searchResults))
            {
                editTagsForm.ShowDialog(this);
            }
        }

        private ListViewItem CreateListItem(SearchResult searchResult)
        {
            var firstPath = searchResult.Paths.FirstOrDefault();

            var columns = new ListViewItem.ListViewSubItem[4];

            columns[PathColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = searchResult.Paths.Count > 0 ? Path.GetFileNameWithoutExtension(firstPath) : searchResult.Hash,
                Tag = firstPath,
            };

            columns[TagsColumnIndex] = new ListViewItem.ListViewSubItem
            {
                Text = string.Join(" ", searchResult.Tags),
                Tag = searchResult.Tags,
            };
            columns[SizeColumnIndex] = new ListViewItem.ListViewSubItem
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

        private async void FavoriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = (IList<SearchResult>)this.itemContextMenu.Tag;
            if (this.favoriteToolStripMenuItem.CheckState == CheckState.Checked)
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

        private void FindDuplicatesMenuItem_Click(object sender, EventArgs e)
        {
            using (var findDuplicatesForm = new FindDuplicatesForm(this.index))
            {
                findDuplicatesForm.ShowDialog(this);
            }
        }

        private void Index_HashPersonAdded(object sender, ItemAddedEventArgs<Tuple<HashPerson, Person>> e)
        {
            this.UpdateSearchResult(e.Item.Item1.Hash, r => r.With(people: r.People.Add(e.Item.Item2)));
        }

        private void Index_HashPersonRemoved(object sender, ItemRemovedEventArgs<HashPerson> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => r.With(people: r.People.RemoveAll(p => p.PersonId == e.Item.PersonId)));
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => r.With(tags: r.Tags.Add(e.Item.Tag)));
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => r.With(tags: r.Tags.Remove(e.Item.Tag)));
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

            this.listView.Sort();
        }

        private async void ListView_DoubleClick(object sender, MouseEventArgs e)
        {
            if (this.listView.HitTest(e.X, e.Y).Item != null)
            {
                var items = this.listView.SelectedItems.Cast<ListViewItem>().ToList();
                foreach (var item in items)
                {
                    foreach (var path in ((SearchResult)item.Tag).Paths)
                    {
                        if (File.Exists(path))
                        {
                            Process.Start(path);
                            break;
                        }
                    }
                }
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
                    var searchResults = this.listView.SelectedItems.Cast<ListViewItem>().Select(i => (SearchResult)i.Tag).ToList();
                    this.favoriteToolStripMenuItem.CheckState = searchResults.All(r => r.Tags.Contains("favorite")) ? CheckState.Checked : CheckState.Unchecked;
                    this.itemContextMenu.Tag = searchResults;
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

        private void RemoveListItem(ListViewItem value)
        {
            this.items.Remove(((SearchResult)value.Tag).Hash);
            this.listView.Items.Remove(value);
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
                var existing = this.listView.Items.Cast<ListViewItem>().ToDictionary(i => ((SearchResult)i.Tag).Hash);
                var newHashes = new HashSet<string>(data.Select(i => i.Hash));

                this.listView.BeginUpdate();
                this.listView.ListViewItemSorter = null;

                foreach (var kvp in existing)
                {
                    if (!newHashes.Contains(kvp.Key))
                    {
                        this.RemoveListItem(kvp.Value);
                    }
                }

                foreach (var item in data)
                {
                    if (existing.TryGetValue(item.Hash, out var existingItem))
                    {
                        UpdateListItem(existingItem, item);
                    }
                    else
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
            this.splitter1.Visible = this.preview.Visible = this.showPreviewMenuItem.Checked;
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
            this.preview.PreviewItem = this.listView.SelectedItems.Count == 1
                ? (SearchResult)this.listView.SelectedItems.Cast<ListViewItem>().Single().Tag
                : null;
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

        private void UpdateSearchResult(string hash, Func<SearchResult, SearchResult> updateSearchResult)
        {
            if (this.items.TryGetValue(hash, out var item))
            {
                var result = (SearchResult)item.Tag;
                var updated = updateSearchResult(result);
                if (!object.ReferenceEquals(result, updated))
                {
                    this.InvokeIfRequired(() => UpdateListItem(item, updated));
                }
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
                    case PathColumnIndex:
                        value = PathComparer.Instance.Compare((string)a.SubItems[PathColumnIndex].Tag, (string)b.SubItems[PathColumnIndex].Tag);
                        break;

                    case TagsColumnIndex:
                        value = ((ImmutableHashSet<string>)a.SubItems[TagsColumnIndex].Tag).Count.CompareTo(((ImmutableHashSet<string>)b.SubItems[TagsColumnIndex].Tag).Count);
                        break;

                    case SizeColumnIndex:
                        value = ((long)a.SubItems[SizeColumnIndex].Tag).CompareTo((long)b.SubItems[SizeColumnIndex].Tag);
                        break;

                    case PeopleColumnIndex:
                        value = ((ImmutableList<Person>)a.SubItems[PeopleColumnIndex].Tag).Count.CompareTo(((ImmutableList<Person>)b.SubItems[PeopleColumnIndex].Tag).Count);
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
