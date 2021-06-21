// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Properties;
    using MediaLibrary.Search;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search;

    public partial class MainForm : Form
    {
        private readonly MediaIndex index;
        private readonly List<Task> tasks = new List<Task>();
        private VirtualSearchResultsView listView;
        private PreviewControl preview;
        private int progressVersion;
        private int searchVersion;
        private List<Form> selectionDialogs = new List<Form>();

        public MainForm(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.index.RescanProgressUpdated += this.Index_RescanProgressUpdated;
            this.InitializeComponent();

            this.listView = new VirtualSearchResultsView(index)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Name = "listView",
                SmallImageList = this.fileTypeImages,
                TabIndex = 4,
                UseCompatibleStateImageBehavior = false,
            };
            this.listView.MouseClick += this.ListView_MouseClick;
            this.listView.MouseDoubleClick += this.ListView_DoubleClick;
            this.listView.SelectionChanged += this.ListView_SelectionChanged;

            this.preview = new PreviewControl(index)
            {
                Dock = DockStyle.Fill,
                Name = "preview",
                TabIndex = 5,
            };

            this.splitContainer.Panel1.Controls.Add(this.listView);
            this.splitContainer.Panel2.Controls.Add(this.preview);

            this.ApplySettings();
        }

        public TimeSpan AutoSearchDelay
        {
            get
            {
                const double KeyboardDelayIncrementSeconds = 0.250;
                var systemKeyboardDelay = TimeSpan.FromSeconds(Math.Max(0.0, Math.Min(9.0, SystemInformation.KeyboardDelay)) * KeyboardDelayIncrementSeconds + KeyboardDelayIncrementSeconds);
                var value = TimeSpan.FromSeconds(Math.Max(systemKeyboardDelay.TotalSeconds, Math.Min(Settings.Default.AutoSearchDelay.TotalSeconds, systemKeyboardDelay.TotalSeconds * 4)));
                return value;
            }
        }

        private static bool CanDrop(DragEventArgs e) =>
            e.AllowedEffect.HasFlag(DragDropEffects.Link) &&
            e.Data.GetDataPresent(DataFormats.FileDrop) &&
            ((string[])e.Data.GetData(DataFormats.FileDrop)).All(Directory.Exists);

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void AddIndexedFolderToolStripMenuItem_Click(object sender, EventArgs e)
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
            this.TrackTask(this.index.AddIndexedPath(selectedPath));
        }

        private void AddPeopleMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = (((ToolStripMenuItem)sender).Tag as IList<SearchResult>) ?? this.listView.SelectedResults;
            if (searchResults.Count > 0)
            {
                this.OpenSelectionDialog(new AddPeopleForm(this.index, searchResults));
            }
        }

        private ToolStripMenuItem AddRatingCategoryMenuItem(string ratingCategory)
        {
            var categoryItem = new ToolStripMenuItem
            {
                Text = ratingCategory,
                Tag = ratingCategory,
                Image = Resources.antique_axe,
            };

            categoryItem.Click += this.RatingCategoryMenuItem_Click;

            this.rateAllButton.DropDownItems.Insert(this.rateAllButton.DropDownItems.Count - 2, categoryItem);
            return categoryItem;
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
            var searchResults = (((ToolStripMenuItem)sender).Tag as IList<SearchResult>) ?? this.listView.SelectedResults;
            if (searchResults.Count > 0)
            {
                this.OpenSelectionDialog(new EditTagsForm(this.index, searchResults));
            }
        }

        private void ApplySettings()
        {
            var settings = Settings.Default;
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

            try
            {
                this.listView.SortColumn = settings.SortColumn;
                this.listView.SortDescending = settings.SortDescending;
            }
            catch (ArgumentException)
            {
            }

            this.listView.ColumnsSettings = settings.Columns;
            this.listView.ColumnWidthChanged += (sender, args) =>
            {
                settings.Columns = this.listView.ColumnsSettings;
                Save();
            };
            this.listView.ColumnReordered += (sender, args) =>
            {
                settings.Columns = this.listView.ColumnsSettings;
                Save();
            };
        }

        private void CloseSelectionDialogs()
        {
            this.selectionDialogs.ForEach(d => d.Dispose());
            this.selectionDialogs.Clear();
        }

        private void CopyHashContextMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = this.listView.SelectedResults;
            if (searchResults.Count > 0)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, searchResults.Select(r => r.Hash)));
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            if (this.searchBox.Focused)
            {
                if (this.searchBox.SelectionLength > 0)
                {
                    Clipboard.SetText(this.searchBox.SelectedText);
                }

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
            this.favoriteMainMenuItem.CheckState = this.listView.SelectedResults.All(r => r.Tags.Contains("favorite")) ? CheckState.Checked : CheckState.Unchecked;
        }

        private async void FavoriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var senderMenu = (ToolStripMenuItem)sender;

            bool @checked;
            if (!(senderMenu.Tag is IList<SearchResult> searchResults))
            {
                searchResults = this.listView.SelectedResults;
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

        private async void FindSimilarMenuItem_Click(object sender, EventArgs e)
        {
            var searchResults = this.listView.SelectedResults;
            var getDetailsTasks = searchResults.Select(r => this.index.GetHashDetails(r.Hash)).ToArray();
            await Task.WhenAll(getDetailsTasks).ConfigureAwait(true);

            var termsFromTasks =
                (from t in getDetailsTasks
                 let details = t.Result
                 where details.ContainsKey(ImageDetailRecognizer.Properties.AverageIntensityHash)
                 let hashObj = details[ImageDetailRecognizer.Properties.AverageIntensityHash]
                 where hashObj is long
                 select new FieldTerm("similar", ((ulong)(long)hashObj).ToString("x16"))).ToList();

            if (termsFromTasks.Count > 0)
            {
                this.Search(new DisjunctionTerm(termsFromTasks));
            }
        }

        private IEnumerable<string> GetSelectedPaths()
        {
            foreach (var item in this.listView.SelectedResults)
            {
                foreach (var path in item.Paths.OrderBy(p => p, PathComparer.Instance))
                {
                    if (File.Exists(PathEncoder.ExtendPath(path)))
                    {
                        yield return path;
                        break;
                    }
                }
            }
        }

        private void Index_RescanProgressUpdated(object sender, ItemUpdatedEventArgs<RescanProgress> args)
        {
            var progress = args.Item;
            var version = Interlocked.Increment(ref this.progressVersion);
            this.InvokeIfRequired(() =>
            {
                if (this.progressVersion == version)
                {
                    this.mainProgressBar.Value = (int)Math.Floor(progress.Estimate * this.mainProgressBar.Maximum);
                    this.mainProgressBar.ToolTipText = $"{progress.Estimate:P0} ({progress.PathsProcessed}/{progress.PathsDiscovered}{(progress.DiscoveryComplete ? string.Empty : "?")})";
                    this.mainProgressBar.Visible = !progress.DiscoveryComplete || progress.Estimate < 1;
                }
            });
        }

        private async void ListView_DoubleClick(object sender, MouseEventArgs e)
        {
            if (this.listView.HitTest(e.X, e.Y).Item != null)
            {
                this.OpenMenuItem_Click(this.openContextMenuItem, e);
            }
        }

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                var item = this.listView.FocusedItem;
                if (item.Bounds.Contains(e.Location))
                {
                    var searchResults = this.listView.SelectedResults;
                    this.favoriteContextMenuItem.CheckState = searchResults.All(r => r.Tags.Contains("favorite")) ? CheckState.Checked : CheckState.Unchecked;
                    this.favoriteContextMenuItem.Tag = searchResults;
                    this.editTagsContextMenuItem.Tag = searchResults;
                    this.addPeopleContextMenuItem.Tag = searchResults;
                    this.findSimilarMenuItem.Enabled = searchResults.All(r => FileTypeHelper.IsImage(r.FileType));
                    this.itemContextMenu.Show(Cursor.Position);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, EventArgs e)
        {
            this.CloseSelectionDialogs();
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
            this.rateAllButton.Enabled = false;
            await this.index.Initialize().ConfigureAwait(true);

            var savedSearches = await this.index.GetAllSavedSearches().ConfigureAwait(true);
            foreach (var savedSearch in savedSearches.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                this.AddSavedSearchMenuItem(savedSearch);
            }

            this.savedSearchesMenuItem.Enabled = true;

            foreach (var ratingCategory in await this.index.GetAllRatingCategories().ConfigureAwait(true))
            {
                this.AddRatingCategoryMenuItem(ratingCategory);
            }

            this.rateAllButton.Enabled = true;

            this.TrackTask(this.index.Rescan());
        }

        private void MergePeopleMenuItem_Click(object sender, EventArgs e)
        {
            using (var mergePeopleForm = new MergePeopleForm(this.index))
            {
                mergePeopleForm.ShowDialog(this);
            }
        }

        private void NewCategoryMenuItem_Click(object sender, EventArgs e)
        {
            using (var nameInputForm = new NameInputForm())
            {
                nameInputForm.Text = "New Category";
                if (nameInputForm.ShowDialog(this) == DialogResult.OK)
                {
                    var menuItem = this.AddRatingCategoryMenuItem(nameInputForm.SelectedName);
                    menuItem.PerformClick();
                }
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var path in this.GetSelectedPaths())
            {
                Process.Start(path);
            }
        }

        private void OpenSelectionDialog(Form form)
        {
            form.Owner = this;
            form.ShowInTaskbar = false;
            form.FormClosed += (s, a) => form.Dispose();
            this.selectionDialogs.Add(form);

            if (form.StartPosition == FormStartPosition.CenterParent)
            {
                form.StartPosition = FormStartPosition.Manual;
                var x = this.Location.X + (this.Width - form.Width) / 2;
                var y = this.Location.Y + (this.Height - form.Height) / 2;
                form.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
            }

            form.Show();
        }

        private void OpenSlideshow(bool shuffle = false, bool autoPlay = false)
        {
            var searchResults = this.listView.SelectedResults;
            if (searchResults.Count <= 1)
            {
                searchResults = this.listView.SearchResults;
            }

            new SlideShowForm(this.index, searchResults, shuffle, autoPlay).Show(this);
        }

        private async Task PerformSearch(bool throttle = false)
        {
            var searchText = this.searchBox.Text;
            var selectedHashes = new HashSet<string>(this.listView.SelectedResults.Select(r => r.Hash));
            var searchVersion = Interlocked.Increment(ref this.searchVersion);
            if (throttle)
            {
                await Task.Delay(this.AutoSearchDelay).ConfigureAwait(true);
                if (this.searchVersion != searchVersion)
                {
                    return;
                }
            }

            IList<SearchResult> data;
            try
            {
                data = await this.index.SearchIndex(searchText).ConfigureAwait(true);
            }
            catch
            {
                data = Array.Empty<SearchResult>();
            }

            if (this.searchVersion == searchVersion)
            {
                this.listView.SearchResults = data;
                this.listView.SelectObjects(data.Where(d => selectedHashes.Contains(d.Hash)).ToList());
            }
        }

        private void PlayAllButton_Click(object sender, EventArgs e)
        {
            this.OpenSlideshow(autoPlay: true);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            this.OpenSlideshow();
        }

        private void RatingCategoryMenuItem_Click(object sender, EventArgs e)
        {
            var category = (sender as ToolStripItem)?.Tag as string ?? string.Empty;

            var searchResults = this.listView.SelectedResults;
            if (searchResults.Count <= 1)
            {
                searchResults = this.listView.SearchResults;
            }

            if (searchResults.Count > 2)
            {
                new CompareForm(this.index, category, searchResults).Show(this);
            }
        }

        private async void RefreshMenuItem_Click(object sender, EventArgs e)
        {
            await this.PerformSearch().ConfigureAwait(true);
        }

        private void SavedSearchMenuItem_Click(object sender, EventArgs e)
        {
            var savedSearch = (SavedSearch)((ToolStripMenuItem)sender).Tag;
            this.viewButton.HideDropDown();
            this.Search(new SavedSearchTerm(savedSearch.Name));
        }

        private async void SaveThisSearchMenuItem_Click(object sender, EventArgs e)
        {
            var searchText = this.searchBox.Text;
            using (var nameInputForm = new NameInputForm())
            {
                nameInputForm.Text = "Save Search";
                if (nameInputForm.ShowDialog(this) == DialogResult.OK)
                {
                    var savedSearch = await this.index.AddSavedSearch(nameInputForm.SelectedName, searchText).ConfigureAwait(true);
                    this.AddSavedSearchMenuItem(savedSearch);
                }
            }
        }

        private void Search(Term searchTerm) => this.Search(searchTerm?.ToString());

        private void Search(string search)
        {
            this.searchBox.Text = search ?? string.Empty;
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

            this.Search(tag);
        }

        private async void SearchBox_TextChangedAsync(object sender, EventArgs e)
        {
            await this.PerformSearch(throttle: true).ConfigureAwait(true);
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

        private void TrackTask(Task task)
        {
            lock (this.tasks)
            {
                this.tasks.Add(task);
            }

            task.ContinueWith(_ =>
            {
                lock (this.tasks)
                {
                    this.tasks.Remove(task);
                }
            }, TaskScheduler.Current);
        }

        private void UpdatePreview()
        {
            this.preview.PreviewItems = this.listView.SelectedResults;
        }
    }
}
