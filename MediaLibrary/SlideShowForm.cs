// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class SlideShowForm : Form
    {
        private readonly MediaIndex index;
        private readonly PlaylistManager<string> playlistManager;
        private readonly Dictionary<string, SearchResult> searchResults;
        private bool advanceOnNextStop;
        private CancellationTokenSource playPauseCancel = new CancellationTokenSource();

        public SlideShowForm(MediaIndex index, IEnumerable<SearchResult> searchResults, bool shuffle = false, bool autoPlay = false)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            var searchResultsList = searchResults.ToList();
            this.searchResults = searchResultsList.ToDictionary(r => r.Hash);
            this.playlistManager = PlaylistManager.Create(searchResultsList.Select(r => r.Hash));
            this.InitializeComponent();

            this.LastMouseMove = Stopwatch.StartNew();
            var mouseMoveEvents = new MouseMoveEventFilter();
            mouseMoveEvents.MouseMove += (sender, args) => this.LastMouseMove = Stopwatch.StartNew();
            this.Shown += (sender, args) => Application.AddMessageFilter(mouseMoveEvents);
            this.FormClosed += (sender, args) => Application.RemoveMessageFilter(mouseMoveEvents);

            this.index.HashPersonAdded += this.Index_HashPersonAdded;
            this.index.HashPersonRemoved += this.Index_HashPersonRemoved;
            this.index.HashTagAdded += this.Index_HashTagAdded;
            this.index.HashTagRemoved += this.Index_HashTagRemoved;

            this.shuffleButton.Checked = shuffle;
            this.playPauseButton.Checked = !autoPlay;
            if (autoPlay)
            {
                this.PlayPauseButton_Click(this, new EventArgs());
            }
            else
            {
                this.playPauseCancel.Cancel();
            }
        }

        public SearchResult Current
        {
            get
            {
                var current = this.playlistManager.Current;
                return current != null && this.searchResults.TryGetValue(current, out var searchResult) ? searchResult : null;
            }
        }

        public TimeSpan ImagePreviewTime { get; set; } = TimeSpan.FromSeconds(8);

        public Stopwatch LastMouseMove { get; private set; }

        public TimeSpan MouseSettleTime { get; set; } = TimeSpan.FromSeconds(2);

        protected override void OnClosed(EventArgs e)
        {
            this.index.HashPersonAdded -= this.Index_HashPersonAdded;
            this.index.HashPersonRemoved -= this.Index_HashPersonRemoved;
            this.index.HashTagAdded -= this.Index_HashTagAdded;
            this.index.HashTagRemoved -= this.Index_HashTagRemoved;
        }

        private async void AdvanceOnMouseSettle(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                var remainingSettleTime = this.MouseSettleTime - this.LastMouseMove.Elapsed;
                if (remainingSettleTime <= TimeSpan.Zero)
                {
                    await this.Next().ConfigureAwait(true);
                    return;
                }
                else
                {
                    await Task.Delay(remainingSettleTime).ConfigureAwait(true);
                }
            }
        }

        private async void FavoriteButton_Click(object sender, EventArgs e)
        {
            var hash = this.playlistManager.Current;
            var senderButton = (ToolStripButton)sender;
            if (senderButton.Checked)
            {
                await this.index.AddHashTag(new HashTag(hash, "favorite")).ConfigureAwait(false);
            }
            else
            {
                await this.index.RemoveHashTag(new HashTag(hash, "favorite")).ConfigureAwait(false);
            }
        }

        private void Index_HashPersonAdded(object sender, ItemAddedEventArgs<(HashPerson hash, Person person)> e)
        {
            this.UpdateSearchResult(e.Item.hash.Hash, r => MediaIndex.UpdateSearchResult(r, e));
        }

        private void Index_HashPersonRemoved(object sender, ItemRemovedEventArgs<HashPerson> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => MediaIndex.UpdateSearchResult(r, e));
        }

        private void Index_HashTagAdded(object sender, ItemAddedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => MediaIndex.UpdateSearchResult(r, e));
        }

        private void Index_HashTagRemoved(object sender, ItemRemovedEventArgs<HashTag> e)
        {
            this.UpdateSearchResult(e.Item.Hash, r => MediaIndex.UpdateSearchResult(r, e));
        }

        private async Task Next()
        {
            if (this.playlistManager.Next())
            {
                var oldCancel = this.playPauseCancel;
                var newCancel = oldCancel;
                if (!oldCancel.IsCancellationRequested)
                {
                    newCancel = new CancellationTokenSource();
                    oldCancel.Cancel();
                    this.playPauseCancel = newCancel;
                }

                this.UpdatePreview();

                if (!newCancel.IsCancellationRequested && PreviewControl.IsImage(this.Current))
                {
                    try
                    {
                        await Task.Delay(this.ImagePreviewTime, newCancel.Token).ConfigureAwait(true);
                        this.AdvanceOnMouseSettle(newCancel.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            await this.Next().ConfigureAwait(false);
        }

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            if (this.playPauseButton.Checked)
            {
                this.playPauseCancel.Cancel();
            }
            else
            {
                this.Resume();
            }
        }

        private void Preview_Finished(object sender, EventArgs e)
        {
            if (!this.playPauseCancel.IsCancellationRequested)
            {
                this.advanceOnNextStop = true;
            }
        }

        private void Preview_PausedOrScannedBackward(object sender, EventArgs e)
        {
            this.advanceOnNextStop = false;
            this.playPauseCancel.Cancel();
            this.playPauseButton.Checked = true;
        }

        private async void Preview_Stopped(object sender, EventArgs e)
        {
            if (this.advanceOnNextStop)
            {
                this.advanceOnNextStop = false;
                var cancel = this.playPauseCancel.Token;
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancel).ConfigureAwait(true);
                    this.AdvanceOnMouseSettle(cancel);
                }
                catch (OperationCanceledException)
                {
                }
            }
            else
            {
                this.Preview_PausedOrScannedBackward(sender, e);
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            this.playPauseButton.Checked = true;
            if (this.playlistManager.Previous())
            {
                this.UpdatePreview();
            }
        }

        private async void Resume()
        {
            if (this.playPauseCancel.IsCancellationRequested)
            {
                this.playPauseCancel = new CancellationTokenSource();
            }

            var current = this.Current;
            if (current != null && !PreviewControl.IsImage(current) && false /* TODO: Current media is assumed to be finished. */)
            {
                // TODO: Resume current media. Until then, user is expected to resume media or click next themselves.
            }
            else
            {
                await this.Next().ConfigureAwait(true);
            }
        }

        private void ShuffleButton_CheckedChanged(object sender, EventArgs e)
        {
            this.playlistManager.Shuffle = this.shuffleButton.Checked;
        }

        private void SlideShowForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.playPauseCancel.Cancel();
        }

        private void SlideShowForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.IsInputKey && !e.Alt && !e.Control && !e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                        this.PreviousButton_Click(sender, e);
                        break;

                    case Keys.Right:
                        this.NextButton_Click(sender, e);
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdatePreview()
        {
            this.advanceOnNextStop = false;
            this.preview.PreviewItem = this.Current;
            this.favoriteButton.Enabled = this.Current != null;
            this.favoriteButton.Checked = this.Current?.Tags?.Contains("favorite") ?? false;
        }

        private void UpdateSearchResult(string hash, Func<SearchResult, SearchResult> updateSearchResult)
        {
            if (this.searchResults.TryGetValue(hash, out var original))
            {
                var result = updateSearchResult(original);
                if (!object.ReferenceEquals(original, result))
                {
                    this.searchResults[hash] = result;
                    if (hash == this.playlistManager.Current)
                    {
                        this.InvokeIfRequired(() => this.UpdatePreview());
                    }
                }
            }
        }

        public class MouseMoveEventFilter : IMessageFilter
        {
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_NCMOUSEMOVE = 0x00a0;

            public event MouseEventHandler MouseMove;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_NCMOUSEMOVE)
                {
                    this.MouseMove?.Invoke(this, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
                }

                return false;
            }
        }
    }
}
