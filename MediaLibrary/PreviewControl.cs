// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
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
        }

        [Category("Data")]
        public SearchResult PreviewItem
        {
            get => this.previewItem;

            set
            {
                this.previewItem = value;

                var isImage = value == null || value.FileType == "image" || value.FileType.StartsWith("image/");
                var url = value == null ? null : (from p in value.Paths
                                                  where File.Exists(p)
                                                  select p).FirstOrDefault();
                if (isImage)
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
                    }
                }
            }
        }
    }
}
