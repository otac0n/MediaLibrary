// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class PreviewControl : UserControl
    {
        private SearchResult displayedItem;
        private SearchResultsTags existingTags;
        private ImmutableList<SearchResult> previewItems;

        public PreviewControl(MediaIndex index)
        {
            this.InitializeComponent();

            this.existingTags = new SearchResultsTags(index);
            this.existingTags.Dock = DockStyle.Bottom;
            this.existingTags.Name = "existingTags";
            this.existingTags.TabIndex = 2;
            this.Controls.Add(this.existingTags);

            this.ResetMediaPlayer();
        }

        public event EventHandler Finished;

        public event EventHandler Paused;

        public event EventHandler Playing;

        public event EventHandler ScannedBackward;

        public event EventHandler ScannedForward;

        public event EventHandler Stopped;

        [Category("Data")]
        public IList<SearchResult> PreviewItems
        {
            get => this.previewItems;

            set
            {
                this.previewItems = value?.ToImmutableList() ?? ImmutableList<SearchResult>.Empty;
                this.UpdatePreview();
            }
        }

        public static bool IsImage(SearchResult searchResult) =>
            searchResult != null && FileTypeHelper.IsImage(searchResult.FileType);

        protected override void OnVisibleChanged(EventArgs e)
        {
            this.UpdatePreview();
            base.OnVisibleChanged(e);
        }

        private void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            switch (this.mediaPlayer.playState)
            {
                case WMPLib.WMPPlayState.wmppsStopped:
                    this.Stopped?.Invoke(this, new EventArgs());
                    break;

                case WMPLib.WMPPlayState.wmppsPaused:
                    this.Paused?.Invoke(this, new EventArgs());
                    break;

                case WMPLib.WMPPlayState.wmppsPlaying:
                    this.Playing?.Invoke(this, new EventArgs());
                    break;

                case WMPLib.WMPPlayState.wmppsScanForward:
                    this.ScannedForward?.Invoke(this, new EventArgs());
                    break;

                case WMPLib.WMPPlayState.wmppsScanReverse:
                    this.ScannedBackward?.Invoke(this, new EventArgs());
                    break;

                case WMPLib.WMPPlayState.wmppsMediaEnded:
                    this.Finished?.Invoke(this, new EventArgs());
                    break;
            }
        }

        private void ResetMediaPlayer()
        {
            this.mediaPlayer.uiMode = "full";
            this.mediaPlayer.enableContextMenu = false;
            this.mediaPlayer.stretchToFit = true;
        }

        private void UpdateMediaPlayerUrl(string url)
        {
            this.mediaPlayer.URL = url;
            var wasVisible = this.mediaPlayer.Visible;
            this.mediaPlayer.Visible = url != null;
            if (this.mediaPlayer.Visible && !wasVisible)
            {
                this.mediaPlayer.Dock = DockStyle.None;
                this.mediaPlayer.Dock = DockStyle.Fill;
                this.ResetMediaPlayer();
            }
        }

        private void UpdatePreview()
        {
            this.existingTags.SearchResults = this.previewItems;
            var item = this.previewItems?.Count == 1 ? this.previewItems.Single() : null;
            var displayedItem = this.Visible ? item : null;
            if (!object.ReferenceEquals(displayedItem, this.displayedItem))
            {
                this.displayedItem = displayedItem;
                var url = displayedItem == null
                    ? null
                    : (from p in displayedItem.Paths
                       let path = PathEncoder.ExtendPath(p)
                       where File.Exists(path)
                       select path).FirstOrDefault();
                if (url == null || IsImage(item))
                {
                    this.UpdateMediaPlayerUrl(null);
                    this.UpdateThumbnailUrl(url);
                }
                else
                {
                    this.UpdateThumbnailUrl(null);
                    this.UpdateMediaPlayerUrl(url);
                }
            }
        }

        private void UpdateThumbnailUrl(string url)
        {
            var previous = this.thumbnail.Image;
            this.thumbnail.Image = url == null ? null : Image.FromFile(url);
            this.thumbnail.Visible = url != null;
            previous?.Dispose();
        }
    }
}
