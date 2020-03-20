// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
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

        protected override void OnClosed(EventArgs e)
        {
            this.index.HashPersonAdded -= this.Index_HashPersonAdded;
            this.index.HashPersonRemoved -= this.Index_HashPersonRemoved;
            this.index.HashTagAdded -= this.Index_HashTagAdded;
            this.index.HashTagRemoved -= this.Index_HashTagRemoved;
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

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (this.playlistManager.Next())
            {
                this.UpdatePreview();
            }
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
                await Task.Delay(100).ConfigureAwait(true);
                this.NextButton_Click(this.nextButton, new EventArgs());
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

        private void Resume()
        {
            this.playPauseCancel.Cancel();
            this.playPauseCancel = new CancellationTokenSource();

            var current = this.Current;
            if (current == null)
            {
                this.NextButton_Click(this.nextButton, new EventArgs());
            }
            else
            {
                // TODO: Resume paused media or resume next if the current media is finished.
            }
        }

        private void ShuffleButton_CheckedChanged(object sender, EventArgs e)
        {
            this.playlistManager.Shuffle = this.shuffleButton.Checked;
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
    }
}
