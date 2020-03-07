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
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using Microsoft.VisualBasic.FileIO;

    public partial class FindDuplicatesForm : Form
    {
        private readonly MediaIndex index;
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private bool initialized;
        private Dictionary<string, (TreeNode node, ListViewItem item)> nodes = new Dictionary<string, (TreeNode, ListViewItem)>();
        private bool running;
        private bool synchronizeTreeView;

        public FindDuplicatesForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private static string FindBestPath(IEnumerable<string> paths)
        {
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
                    if (path.Length < minFile.Length)
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
            var copySuffixes = new Regex(@"\G(?:\(Copy\)|\s|.temp|.tmp)");
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
                this.Hide();
            }

            this.Hide();
        }

        private void DuplicatesList_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            this.nodes[(string)e.Item.Tag].node.Checked = e.Item.Checked;
            this.UpdateGroup(e.Item.Group);
            this.UpdateChart();
        }

        private void FindDuplicatesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.cancel.Cancel();
        }

        private async void FindDuplicatesForm_Load(object sender, System.EventArgs e)
        {
            var results = await this.index.SearchIndex("copies>1").ConfigureAwait(true);

            this.duplicatesList.ItemChecked -= this.DuplicatesList_ItemChecked;
            this.duplicatesList.BeginUpdate();

            var nodes = results.SelectMany(r => r.Paths).ToDictionary(p => p, p => new TreeNode(Path.GetFileName(p)) { Tag = p, ImageKey = "none", SelectedImageKey = "none" });
            var queue = nodes.Select(n => new { Key = Path.GetDirectoryName(n.Key), Node = n.Value }).ToList();
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
                    node = new TreeNode(Path.GetFileName(key = group.Key)) { ImageKey = "folder-empty", SelectedImageKey = "folder-empty" };
                    foreach (var item in items.OrderBy(i => i.Node.Nodes.Count == 0).ThenBy(i => i.Node.Text))
                    {
                        queue.Remove(item);
                        node.Nodes.Add(item.Node);
                    }
                }

                var ix = key.LastIndexOfAny(PathComparer.PathSeparators);
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

            foreach (var result in results.OrderByDescending(r => r.FileSize * (r.Paths.Count - 1)))
            {
                var group = new ListViewGroup($"{result.Hash} ({ByteSize.FromBytes(result.FileSize)} × {result.Paths.Count})")
                {
                    Tag = result,
                };
                this.duplicatesList.Groups.Add(group);

                foreach (var path in result.Paths.OrderBy(r => r, PathComparer.Instance))
                {
                    var node = nodes[path];
                    var item = new ListViewItem(path, group)
                    {
                        Checked = false,
                        Tag = path,
                    };

                    this.nodes[path] = (node, item);
                    this.duplicatesList.Items.Add(item);
                }
            }

            if (results.Count > 0)
            {
                this.duplicatesList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }

            this.treeView.Enabled = this.duplicatesList.Enabled = this.okButton.Enabled = true;
            this.duplicatesList.EndUpdate();
            this.duplicatesList.ItemChecked += this.DuplicatesList_ItemChecked;

            foreach (var result in results)
            {
                var bestPath = FindBestPath(result.Paths);
                if (bestPath != null)
                {
                    this.nodes[bestPath].item.Checked = true;
                }
            }

            this.initialized = true;
            this.UpdateChart();
        }

        private async void OKButton_Click(object sender, System.EventArgs e)
        {
            var totalBytes = (from g in this.duplicatesList.Groups.Cast<ListViewGroup>()
                              let result = (SearchResult)g.Tag
                              let toKeep = g.Items.Cast<ListViewItem>().ToLookup(i => i.Checked)
                              where toKeep[true].Any()
                              from i in toKeep[false]
                              select result.FileSize).Sum();
            var progressBytes = 0L;
            this.progressBar.Value = 0;

            this.SetRunning(true);

            var allGroups = this.duplicatesList.Groups.Cast<ListViewGroup>().ToList();
            for (var g = 0; g < allGroups.Count; g++)
            {
                if (this.cancel.IsCancellationRequested)
                {
                    break;
                }

                var group = allGroups[g];
                var result = (SearchResult)group.Tag;
                var items = group.Items.Cast<ListViewItem>().ToLookup(i => i.Checked);
                var toKeep = items[true].Select(i => (string)i.Tag).ToList();
                var toRemove = items[false].Select(i => (string)i.Tag).ToList();

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
                    progressBytes += toRemove.Count * result.FileSize;
                    this.progressBar.Value = totalBytes != 0
                        ? (int)(progressBytes * this.progressBar.Maximum / totalBytes)
                        : (int)((g + 1L) * this.progressBar.Maximum / allGroups.Count);
                }

                if (success)
                {
                    void RemoveTreeNode(ListViewItem removed)
                    {
                        var path = (string)removed.Tag;
                        var node = this.nodes[path].node;
                        this.nodes.Remove(path);

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

                    foreach (var removed in items[false])
                    {
                        RemoveTreeNode(removed);
                        this.duplicatesList.Items.Remove(removed);
                    }

                    if (toKeep.Count == 1)
                    {
                        foreach (var removed in items[true])
                        {
                            RemoveTreeNode(removed);
                            this.duplicatesList.Items.Remove(removed);
                        }

                        this.duplicatesList.Groups.Remove(group);
                    }
                }
            }

            this.SetRunning(false);
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
                    FileSystem.DeleteFile(r, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }

            foreach (var file in removed)
            {
                await this.index.RemoveFilePath(file).ConfigureAwait(false);
            }

            return true;
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
            if (node.Tag is string path)
            {
                this.nodes[path].item.Checked = node.Checked;
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

            if (this.duplicatesList.Groups.Count == 0)
            {
                this.sizeChart.Series[0].Points.Clear();
                return;
            }

            var sizeNecessary = 0L;
            var sizeKeepRedundant = 0L;
            var sizeRedundantUnclassified = 0L;
            var sizeToDelete = 0L;

            foreach (ListViewGroup group in this.duplicatesList.Groups)
            {
                var result = (SearchResult)group.Tag;
                var @checked = group.Items.Cast<ListViewItem>().Count(i => i.Checked);

                sizeNecessary += result.FileSize;
                if (@checked == 0)
                {
                    sizeRedundantUnclassified += (result.Paths.Count - 1) * result.FileSize;
                }
                else
                {
                    sizeKeepRedundant += (@checked - 1) * result.FileSize;
                    sizeToDelete += (result.Paths.Count - @checked) * result.FileSize;
                }
            }

            var labels = new List<string>();
            var values = new List<long>();

            labels.Add($"Necessary ({ByteSize.FromBytes(sizeNecessary)})");
            values.Add(sizeNecessary);
            labels.Add($"Extra Copies Kept ({ByteSize.FromBytes(sizeKeepRedundant)})");
            values.Add(sizeKeepRedundant);
            labels.Add($"To Delete ({ByteSize.FromBytes(sizeToDelete)})");
            values.Add(sizeToDelete);
            labels.Add($"Redundant Copies ({ByteSize.FromBytes(sizeRedundantUnclassified)})");
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

        private void UpdateGroup(ListViewGroup group)
        {
            var noneChecked = !group.Items.Cast<ListViewItem>().Any(i => i.Checked);
            foreach (ListViewItem item in group.Items)
            {
                var treeNode = this.nodes[(string)item.Tag].node;
                treeNode.ImageKey = treeNode.SelectedImageKey = item.ImageKey =
                    noneChecked ? "none" :
                    item.Checked ? "save" :
                    "delete";
                this.UpdateFolderImage(treeNode.Parent);
            }
        }
    }
}
