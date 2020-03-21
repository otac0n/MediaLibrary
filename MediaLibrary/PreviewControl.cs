// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Storage.Search;

    public partial class PreviewControl : UserControl
    {
        private SearchResult previewItem;

        public PreviewControl()
        {
            this.InitializeComponent();
            this.ResetMediaPlayer();
        }

        public event EventHandler Finished;

        public event EventHandler Paused;

        public event EventHandler Playing;

        public event EventHandler ScannedBackward;

        public event EventHandler ScannedForward;

        public event EventHandler Stopped;

        [Category("Data")]
        public SearchResult PreviewItem
        {
            get => this.previewItem;

            set
            {
                this.previewItem = value;

                var url = value == null
                    ? null
                    : (from p in value.Paths
                       where File.Exists(p)
                       select p).FirstOrDefault();
                if (value == null || IsImage(value))
                {
                    this.mediaPlayer.URL = null;
                    this.mediaPlayer.Visible = false;
                    this.thumbnail.ImageLocation = url;
                    this.thumbnail.Visible = url != null;
                }
                else
                {
                    this.thumbnail.ImageLocation = null;
                    this.thumbnail.Visible = false;
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
            }
        }

        public static bool IsImage(SearchResult searchResult) =>
            searchResult != null && (searchResult.FileType == "image" || searchResult.FileType.StartsWith("image/", StringComparison.Ordinal));

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
    }
}
