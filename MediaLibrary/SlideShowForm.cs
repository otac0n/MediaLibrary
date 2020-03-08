// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class SlideShowForm : Form
    {
        private readonly MediaIndex index;
        private PlaylistManager<SearchResult> playlistManager;

        public SlideShowForm(MediaIndex index, IEnumerable<SearchResult> searchResults, bool shuffle = false, bool autoPlay = false)
        {
            this.playlistManager = PlaylistManager.Create(searchResults);
            this.InitializeComponent();
            this.shuffleButton.Checked = shuffle;
            this.preview.PreviewItem = this.playlistManager.Current;
            this.index = index;
        }

        protected override void OnClosed(EventArgs e)
        {
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
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            // TODO: Pause?

            if (this.playlistManager.Previous())
            {
                this.UpdatePreview();
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
            this.preview.PreviewItem = this.playlistManager.Current;
        }
    }
}
