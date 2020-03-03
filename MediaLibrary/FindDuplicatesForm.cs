// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ByteSizeLib;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using Microsoft.VisualBasic.FileIO;

    public partial class FindDuplicatesForm : Form
    {
        private static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly MediaIndex index;
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private Dictionary<string, (TreeNode node, ListViewItem item)> nodes = new Dictionary<string, (TreeNode, ListViewItem)>();
        private bool running;
        private bool synchronizeTreeView;

        public FindDuplicatesForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
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
                    node = new TreeNode(Path.GetFileName(key = group.Key)) { ImageKey = "none", SelectedImageKey = "none" };
                    foreach (var item in items.OrderBy(i => i.Node.Nodes.Count == 0).ThenBy(i => i.Node.Text))
                    {
                        queue.Remove(item);
                        node.Nodes.Add(item.Node);
                    }
                }

                var ix = key.LastIndexOfAny(PathSeparators);
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

            foreach (var result in results.OrderByDescending(r => r.FileSize * (r.Paths.Length - 1)))
            {
                var group = new ListViewGroup($"{result.Hash} ({ByteSize.FromBytes(result.FileSize)} × {result.Paths.Length})")
                {
                    Tag = result,
                };
                this.duplicatesList.Groups.Add(group);

                foreach (var path in result.Paths.OrderBy(r => r, PathComparer.Instance))
                {
                    var item = new ListViewItem(path, group)
                    {
                        Checked = false,
                        Tag = path,
                    };

                    this.nodes[path] = (nodes[path], item);
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
            foreach (var group in allGroups)
            {
                if (this.cancel.IsCancellationRequested)
                {
                    break;
                }

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
                    this.progressBar.Value = (int)(progressBytes * this.progressBar.Maximum / totalBytes);
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
            if (this.synchronizeTreeView)
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
                    sizeRedundantUnclassified += (result.Paths.Length - 1) * result.FileSize;
                }
                else
                {
                    sizeKeepRedundant += (@checked - 1) * result.FileSize;
                    sizeToDelete += (result.Paths.Length - @checked) * result.FileSize;
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
            }
        }

        private class PathComparer : IComparer<string>
        {
            private PathComparer()
            {
            }

            public static PathComparer Instance { get; } = new PathComparer();

            public int Compare(string aPath, string bPath)
            {
                var aParts = aPath.ToUpperInvariant().Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
                var bParts = bPath.ToUpperInvariant().Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

                var num = 0;
                for (var j = 0; j < aParts.Length && j < bParts.Length; j++)
                {
                    if (aParts.Length != bParts.Length)
                    {
                        if (j == aParts.Length - 1)
                        {
                            return 1;
                        }
                        else if (j == bParts.Length - 1)
                        {
                            return -1;
                        }
                    }

                    if ((num = string.Compare(aParts[j], bParts[j], StringComparison.CurrentCultureIgnoreCase)) != 0)
                    {
                        return num;
                    }
                }

                return 0;
            }
        }
    }
}
