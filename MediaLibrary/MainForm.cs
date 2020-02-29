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
    using MediaLibrary.Storage;

    public partial class MainForm : Form
    {
        private readonly MediaIndex index;
        private readonly List<InProgressTask> tasks = new List<InProgressTask>();
        private bool columnsAutoSized = false;
        private double lastProgress;
        private int searchVersion;
        private int taskVersion;

        public MainForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
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

        private string GetImageKey(HashInfo item)
        {
            switch (item.FileType)
            {
                case "audio/midi": return "audio-file-midi";
                case "audio/mpeg": return "audio-file-mp3";
                case "audio/wav": return "audio-file-wav";
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
                case "video/x-msvideo": return "video-file-avi";
                case "video":
                case string type when type.StartsWith("video/", StringComparison.InvariantCulture):
                    return "video-file";

                default: return "common-file";
            }
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
            var data = await this.index.SearchIndex(this.searchBox.Text).ConfigureAwait(true);
            if (this.searchVersion == searchVersion)
            {
                var existing = this.listView.Items.Cast<ListViewItem>().ToDictionary(i => (string)i.Tag);
                var newHashes = new HashSet<string>(data.Select(i => i.Hash));

                this.listView.BeginUpdate();

                foreach (var kvp in existing)
                {
                    if (!newHashes.Contains(kvp.Key))
                    {
                        this.listView.Items.Remove(kvp.Value);
                    }
                }

                foreach (var item in data)
                {
                    if (!existing.ContainsKey(item.Hash))
                    {
                        this.listView.Items.Add(new ListViewItem(new[] { item.Hash }, this.GetImageKey(item)) { Tag = item.Hash });
                    }
                }

                if (!this.columnsAutoSized && data.Count > 0)
                {
                    this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    this.columnsAutoSized = true;
                }

                this.listView.EndUpdate();
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
    }
}
