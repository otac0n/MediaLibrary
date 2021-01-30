// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ByteSizeLib;
    using MediaLibrary.Search;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class FindDuplicatesForm : Form
    {
        private readonly MediaIndex index;
        private readonly Dictionary<string, PathModel> pathModels = new Dictionary<string, PathModel>();
        private readonly Dictionary<string, ResultModel> resultModels = new Dictionary<string, ResultModel>();
        private readonly PredicateSearchCompiler searchCompiler;
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private bool initialized;
        private bool running;
        private int searchVersion;
        private bool synchronizeTreeView;
        private Predicate<SearchResult> visiblePredicate;

        public FindDuplicatesForm(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.searchCompiler = new PredicateSearchCompiler(index.TagEngine, excludeHidden: false, _ => null); // TODO: Support saved searches.
            this.visiblePredicate = x => true;
            this.InitializeComponent();
        }

        private static string FindBestPath(IEnumerable<string> paths)
        {
            // Get the shortest file, but only if all files are in the same folder.
            string sharedDir = null;
            string minFile = null;
            var otherPaths = new List<string>();
            foreach (var path in paths)
            {
                var dir = Path.GetDirectoryName(path);
                if (sharedDir == null)
                {
                    sharedDir = dir;
                    minFile = path;
                }
                else if (sharedDir == dir)
                {
                    if (path.Length < minFile.Length ||
                        (path.Length == minFile.Length && StringComparer.OrdinalIgnoreCase.Compare(path, minFile) < 0))
                    {
                        otherPaths.Add(minFile);
                        minFile = path;
                    }
                    else
                    {
                        otherPaths.Add(path);
                    }
                }
                else
                {
                    return null;
                }
            }

            // For all files in the same foder,
            var minFileName = Path.GetFileName(minFile);
            foreach (var path in otherPaths)
            {
                var fileName = Path.GetFileName(path);
                if (!IsCopyFileName(minFileName, fileName))
                {
                    return null;
                }
            }

            return minFile;
        }

        private static bool IsCopyFileName(string sourceName, string copyName)
        {
            var copyPrefixes = new Regex(@"\G(?:Copy of )");
            var copySuffixes = new Regex(@"\G(?:\(Copy\)|-\s?Copy|\s|.temp|.tmp)");
            var valueSuffix = new Regex(@"\G\((?<value>\d)\)");

            bool IsCopyFileName(int sourceIndex = 0, int copyIndex = 0)
            {
                Debug.Assert(copyIndex <= copyName.Length);
                if (copyIndex == copyName.Length)
                {
                    return sourceIndex == sourceName.Length;
                }

                if (sourceIndex < sourceName.Length && sourceName[sourceIndex] == copyName[copyIndex] &&
                    IsCopyFileName(sourceIndex + 1, copyIndex + 1))
                {
                    return true;
                }

                var match = (sourceIndex == 0 ? copyPrefixes : copySuffixes).Match(copyName, copyIndex);
                if (match.Success)
                {
                    if (IsCopyFileName(sourceIndex, copyIndex + match.Length))
                    {
                        return true;
                    }
                }

                match = valueSuffix.Match(copyName, copyIndex);
                if (match.Success)
                {
                    if (sourceIndex < sourceName.Length && long.TryParse(match.Groups["value"].Value, out var copyValue))
                    {
                        var sourceMatch = valueSuffix.Match(sourceName, sourceIndex);
                        if (sourceMatch.Success && long.TryParse(sourceMatch.Groups["value"].Value, out var sourceValue) &&
                            IsCopyFileName(sourceIndex + sourceMatch.Length, copyIndex + match.Length))
                        {
                            return true;
                        }
                    }

                    if (IsCopyFileName(sourceIndex, copyIndex + match.Length))
                    {
                        return true;
                    }
                }

                return false;
            }

            return IsCopyFileName();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (this.running)
            {
                this.cancel.Cancel();
            }

            this.Hide();
        }

        private void CreateViewNodes(List<SearchResult> results)
        {
            foreach (var result in results)
            {
                var group = new ListViewGroup($"{result.Hash} ({ByteSize.FromBytes(result.FileSize)} × {result.Paths.Count})")
                {
                    Tag = result,
                };

                this.resultModels.Add(result.Hash, new ResultModel(result)
                {
                    ListViewGroup = group,
                });

                foreach (var path in result.Paths)
                {
                    var filteredResult = result.With(
                        paths: ImmutableHashSet.Create<string>(path));

                    var treeNode = new TreeNode(Path.GetFileName(path))
                    {
                        Tag = path,
                        ImageKey = "none",
                        SelectedImageKey = "none",
                    };

                    var listViewItem = new ListViewItem(path, group)
                    {
                        Checked = false,
                        Tag = path,
                    };

                    this.pathModels.Add(path, new PathModel(path, filteredResult)
                    {
                        TreeNode = treeNode,
                        ListViewItem = listViewItem,
                    });
                }
            }
        }

        private void DuplicatesList_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            var node = this.pathModels[(string)e.Item.Tag].TreeNode;
            if (node.Checked != e.Item.Checked)
            {
                node.Checked = e.Item.Checked;
            }
        }

        private void FindDuplicatesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.cancel.Cancel();
        }

        private async void FindDuplicatesForm_Load(object sender, System.EventArgs e)
        {
            var results = await this.index.SearchIndex("copies>1", excludeHidden: false).ConfigureAwait(true);
            this.UpdateSearchResults(results);
        }

        private async void OKButton_Click(object sender, System.EventArgs e)
        {
            var totalBytes = (from rm in this.resultModels.Values
                              let result = rm.SearchResult
                              let toKeep = rm.RemainingPaths.Select(p => this.pathModels[p]).ToLookup(i => i.ListViewItem.Checked)
                              where toKeep[true].Any()
                              from i in toKeep[false]
                              select result.FileSize).Sum();
            var progressBytes = 0L;
            this.progressBar.Value = 0;

            this.SetRunning(true);

            var anyRemoved = false;
            var allGroups = this.resultModels.Values.ToList();
            for (var g = 0; g < allGroups.Count; g++)
            {
                if (this.cancel.IsCancellationRequested)
                {
                    break;
                }

                var rm = allGroups[g];
                var result = rm.SearchResult;
                var models = rm.RemainingPaths.Select(p => this.pathModels[p]).ToLookup(i => i.ListViewItem.Checked);
                var toKeep = models[true].Select(i => i.Path).ToList();
                var toRemove = models[false].Select(i => i.Path).ToList();

                if (toRemove.Count == 0)
                {
                    continue;
                }

                bool success;
                try
                {
                    success = await this.SafeDelete(expectedHash: result.Hash, keep: toKeep, remove: toRemove).ConfigureAwait(true);
                }
                catch
                {
                    success = false;
                }

                if (toKeep.Count > 0)
                {
                    anyRemoved = true;
                    progressBytes += toRemove.Count * result.FileSize;
                    this.progressBar.Value = totalBytes != 0
                        ? (int)(progressBytes * this.progressBar.Maximum / totalBytes)
                        : (int)((g + 1L) * this.progressBar.Maximum / allGroups.Count);
                }

                if (success)
                {
                    void RemovePathModel(PathModel removed)
                    {
                        var path = removed.Path;
                        var node = removed.TreeNode;
                        var item = removed.ListViewItem;
                        var hash = removed.FilteredResult.Hash;

                        this.pathModels.Remove(path);
                        var resultModel = this.resultModels[hash];
                        resultModel.RemainingPaths.Remove(path);
                        if (resultModel.RemainingPaths.Count == 0)
                        {
                            this.resultModels.Remove(hash);
                        }

                        var group = resultModel.ListViewGroup;
                        this.duplicatesList.Items.Remove(item);
                        if (group.Items.Count == 0)
                        {
                            this.duplicatesList.Groups.Remove(group);
                        }

                        while (node != null && node.Nodes.Count == 0)
                        {
                            var parent = node.Parent;
                            node.Remove();
                            node = parent;
                        }

                        if (node != null)
                        {
                            this.UpdateFolderImage(node);
                        }
                    }

                    foreach (var removed in models[false])
                    {
                        RemovePathModel(removed);
                    }

                    if (toKeep.Count == 1)
                    {
                        foreach (var removed in models[true])
                        {
                            RemovePathModel(removed);
                        }
                    }
                }
            }

            this.SetRunning(false);

            if (!anyRemoved)
            {
                this.Hide();
            }
        }

        private async Task<bool> SafeDelete(string expectedHash, List<string> keep, List<string> remove)
        {
            if (keep == null)
            {
                throw new ArgumentNullException(nameof(keep));
            }
            else if (remove == null)
            {
                throw new ArgumentNullException(nameof(remove));
            }
            else if (keep.Count == 0)
            {
                return false;
            }
            else if (remove.Count == 0)
            {
                return true;
            }

            foreach (var k in keep)
            {
                if ((await MediaIndex.HashFileAsync(k).ConfigureAwait(false)).Hash != expectedHash)
                {
                    return false;
                }
            }

            var removed = new HashSet<string>();
            foreach (var r in remove)
            {
                try
                {
                    if ((await MediaIndex.HashFileAsync(r).ConfigureAwait(false)).Hash != expectedHash)
                    {
                        return false;
                    }
                }
                catch (FileNotFoundException)
                {
                    removed.Add(r);
                    continue;
                }
            }

            foreach (var r in remove)
            {
                if (removed.Add(r))
                {
                    NativeMethods.DeleteToRecycleBin(r);
                }
            }

            foreach (var file in removed)
            {
                await this.index.RemoveFilePath(file).ConfigureAwait(false);
            }

            return true;
        }

        private async void SearchBox_TextChangedAsync(object sender, EventArgs e)
        {
            var query = this.searchBox.Text;
            var searchVersion = Interlocked.Increment(ref this.searchVersion);
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(true);
            if (this.searchVersion != searchVersion)
            {
                return;
            }

            Predicate<SearchResult> predicate;
            try
            {
                var term = new SearchGrammar().Parse(query ?? string.Empty);
                predicate = this.searchCompiler.Compile(term);
            }
            catch
            {
                predicate = r => false;
            }

            if (this.searchVersion == searchVersion)
            {
                this.visiblePredicate = predicate;
                this.UpdateListView();
            }
        }

        private void SetRunning(bool running) => this.treeView.Enabled = this.duplicatesList.Enabled = this.okButton.Enabled = !(this.progressBar.Visible = this.running = running);

        private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            void UpdateParent(TreeNode parent)
            {
                if (parent != null)
                {
                    var value = parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                    if (parent.Checked != value)
                    {
                        parent.Checked = value;
                        UpdateParent(parent.Parent);
                    }
                }
            }

            void UpdateChildren(TreeNode parent)
            {
                foreach (TreeNode child in parent.Nodes)
                {
                    child.Checked = parent.Checked;
                    UpdateChildren(child);
                }
            }

            var node = e.Node;
            var path = (string)node.Tag;
            if (path != null)
            {
                var pathModel = this.pathModels[path];
                pathModel.ListViewItem.Checked = node.Checked;
                this.UpdateGroup(pathModel.FilteredResult.Hash);
            }

            if (!this.synchronizeTreeView)
            {
                this.synchronizeTreeView = true;
                UpdateChildren(node);
                UpdateParent(node.Parent);
                this.synchronizeTreeView = false;
                this.UpdateChart();
            }
        }

        private void UpdateChart()
        {
            if (!this.initialized || this.synchronizeTreeView)
            {
                return;
            }

            if (this.resultModels.Count == 0)
            {
                this.sizeChart.Series[0].Points.Clear();
                return;
            }

            var countNecessary = 0;
            var sizeNecessary = 0L;
            var countKeepRedundant = 0;
            var sizeKeepRedundant = 0L;
            var countRedundantUnclassified = 0L;
            var sizeRedundantUnclassified = 0L;
            var countToDelete = 0L;
            var sizeToDelete = 0L;

            foreach (var pair in this.resultModels.Values)
            {
                var result = pair.SearchResult;
                var @checked = pair.RemainingPaths.Select(p => this.pathModels[p].ListViewItem).Count(i => i.Checked);

                countNecessary += 1;
                sizeNecessary += result.FileSize;

                if (@checked == 0)
                {
                    countRedundantUnclassified += result.Paths.Count - 1;
                    sizeRedundantUnclassified += (result.Paths.Count - 1) * result.FileSize;
                }
                else
                {
                    countKeepRedundant += @checked - 1;
                    sizeKeepRedundant += (@checked - 1) * result.FileSize;
                    countToDelete += result.Paths.Count - @checked;
                    sizeToDelete += (result.Paths.Count - @checked) * result.FileSize;
                }
            }

            var labels = new List<string>();
            var values = new List<long>();

            labels.Add($"Necessary ({countNecessary} files, {ByteSize.FromBytes(sizeNecessary)})");
            values.Add(sizeNecessary);
            labels.Add($"Extra Copies Kept ({countKeepRedundant} files, {ByteSize.FromBytes(sizeKeepRedundant)})");
            values.Add(sizeKeepRedundant);
            labels.Add($"To Delete ({countToDelete} files, {ByteSize.FromBytes(sizeToDelete)})");
            values.Add(sizeToDelete);
            labels.Add($"Redundant Copies ({countRedundantUnclassified} files, {ByteSize.FromBytes(sizeRedundantUnclassified)})");
            values.Add(sizeRedundantUnclassified);

            this.sizeChart.Series[0].Points.DataBindXY(labels, values);
        }

        private void UpdateFolderImage(TreeNode parent)
        {
            var images = new HashSet<string>(parent.Nodes.Cast<TreeNode>().Select(c => c.ImageKey));
            var none = images.Contains("none") || images.Contains("folder-none");
            var save = images.Contains("save") || images.Contains("folder-save");
            var delete = images.Contains("delete") || images.Contains("folder-delete");
            var both = images.Contains("folder-both");

            var originalKey = parent.ImageKey;
            var nextKey =
                both || (save && delete) ? "folder-both" :
                save ? "folder-save" :
                delete ? "folder-delete" :
                "folder-none";
            if (originalKey != nextKey)
            {
                parent.ImageKey = parent.SelectedImageKey = nextKey;
                if (parent.Parent != null)
                {
                    this.UpdateFolderImage(parent.Parent);
                }
            }
        }

        private void UpdateGroup(string hash)
        {
            var group = this.resultModels[hash];
            var items = group.RemainingPaths.Select(p => this.pathModels[p]).ToList();
            var noneChecked = !items.Any(i => i.ListViewItem.Checked);
            foreach (var pathModel in items)
            {
                var item = pathModel.ListViewItem;
                var treeNode = pathModel.TreeNode;
                treeNode.ImageKey = treeNode.SelectedImageKey = item.ImageKey =
                    noneChecked ? "none" :
                    item.Checked ? "save" :
                    "delete";
                this.UpdateFolderImage(treeNode.Parent);
            }
        }

        private void UpdateListView()
        {
            this.duplicatesList.ItemChecked -= this.DuplicatesList_ItemChecked;
            this.duplicatesList.BeginUpdate();
            this.duplicatesList.Items.Clear();
            this.duplicatesList.Groups.Clear();

            var predicate = this.visiblePredicate;
            var added = new HashSet<string>();
            foreach (var pathModel in this.pathModels.Values.OrderByDescending(m => m.FilteredResult.FileSize))
            {
                var item = pathModel.ListViewItem;
                var filteredResult = pathModel.FilteredResult;
                if (predicate(filteredResult))
                {
                    var hash = filteredResult.Hash;
                    var group = this.resultModels[hash].ListViewGroup;
                    if (added.Add(hash))
                    {
                        this.duplicatesList.Groups.Add(group);
                    }

                    item.Group = group;
                    this.duplicatesList.Items.Add(item);
                }
            }

            this.duplicatesList.EndUpdate();
            this.duplicatesList.ItemChecked += this.DuplicatesList_ItemChecked;
        }

        private void UpdateSearchResults(List<SearchResult> results)
        {
            this.CreateViewNodes(results);
            this.UpdateTreeView();
            this.UpdateListView();
            this.treeView.Enabled = this.duplicatesList.Enabled = this.okButton.Enabled = true;

            foreach (var result in results)
            {
                var bestPath = FindBestPath(result.Paths);
                if (bestPath != null)
                {
                    this.pathModels[bestPath].ListViewItem.Checked = true;
                }
            }

            this.duplicatesList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.initialized = true;
            this.UpdateChart();
        }

        private void UpdateTreeView()
        {
            var queue = this.pathModels.Values.Select(p => new { Key = Path.GetDirectoryName(p.Path), Node = p.TreeNode }).ToList();
            var roots = new List<TreeNode>();
            while (queue.Count > 1)
            {
                var group = (from n in queue
                             group n by n.Key into g
                             group g by g.Key.Length into g2
                             orderby g2.Key descending
                             select g2)
                            .First()
                            .OrderBy(g => g.Key, PathComparer.Instance)
                            .First();
                var items = group.ToList();

                string key;
                TreeNode node;
                if (items.Count == 1 && items[0].Node.Nodes.Count > 0)
                {
                    var item = items[0];
                    queue.Remove(item);
                    node = item.Node;
                    key = item.Key;
                    node.Text = Path.GetFileName(key) + Path.DirectorySeparatorChar + node.Text;
                }
                else
                {
                    node = new TreeNode(Path.GetFileName(key = group.Key)) { ImageKey = "folder-none", SelectedImageKey = "folder-none" };
                    foreach (var item in items.OrderBy(i => i.Node.Nodes.Count == 0).ThenBy(i => i.Node.Text))
                    {
                        queue.Remove(item);
                        node.Nodes.Add(item.Node);
                    }
                }

                var ix = key.LastIndexOfAny(MediaIndex.PathSeparators);
                if (ix == -1)
                {
                    roots.Add(node);
                }
                else
                {
                    queue.Add(new { Key = key.Substring(0, ix), Node = node });
                }
            }

            roots.AddRange(queue.Select(n => n.Node));
            roots.ForEach(r => this.treeView.Nodes.Add(r));
        }

        private class PathModel
        {
            public PathModel(string path, SearchResult filteredResult)
            {
                this.Path = path;
                this.FilteredResult = filteredResult;
            }

            public SearchResult FilteredResult { get; }

            public ListViewItem ListViewItem { get; set; }

            public string Path { get; }

            public TreeNode TreeNode { get; set; }
        }

        private class ResultModel
        {
            public ResultModel(SearchResult searchResult)
            {
                this.SearchResult = searchResult;
                this.RemainingPaths = new HashSet<string>(searchResult.Paths);
            }

            public ListViewGroup ListViewGroup { get; set; }

            public HashSet<string> RemainingPaths { get; }

            public SearchResult SearchResult { get; }
        }
    }
}
