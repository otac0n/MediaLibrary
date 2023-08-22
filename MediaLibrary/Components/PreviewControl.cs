// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Properties;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using MediaPlayer = AxWMPLib.AxWindowsMediaPlayer;

    public partial class PreviewControl : UserControl
    {
        private static readonly Display[] MediaDisplays =
        {
            new Display(
                canShow: items => items.Count == 1 && IsImage(items[0]),
                create: (items, parent) => new ImagePreviewControl { Dock = DockStyle.Fill, Visible = false },
                update: (items, control) =>
                {
                    var thumbnail = (ImagePreviewControl)control;
                    var url = items.Count == 1 ? FirstExistingPath(items[0]) : null;
                    var previous = thumbnail.Image;
                    thumbnail.Image = url == null ? null : Image.FromFile(url);
                    thumbnail.Visible = url != null;
                    previous?.Dispose();
                }),
            new Display(
                canShow: items => items.Count == 1 && !IsImage(items[0]),
                create: (items, parent) =>
                {
                    var player = new MediaPlayer();
                    player.BeginInit();
                    player.Dock = DockStyle.Fill;
                    player.Visible = false;

                    var reentrant = false;
                    player.PlayStateChange += (object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e) =>
                    {
                        switch (player.playState)
                        {
                            case WMPLib.WMPPlayState.wmppsStopped:
                                if (!reentrant)
                                {
                                    parent.Stopped?.Invoke(parent, new EventArgs());
                                }

                                break;

                            case WMPLib.WMPPlayState.wmppsPaused:
                                parent.Paused?.Invoke(parent, new EventArgs());
                                break;

                            case WMPLib.WMPPlayState.wmppsPlaying:
                                if (!reentrant)
                                {
                                    var screen = Screen.FromControl(player);
                                    if (!screen.Primary & player.Ctlcontrols.currentPosition == 0)
                                    {
                                        reentrant = true;
                                        player.Ctlcontrols.stop();
                                        player.Ctlcontrols.play();
                                        reentrant = false;
                                    }

                                    parent.Playing?.Invoke(parent, new EventArgs());
                                }

                                break;

                            case WMPLib.WMPPlayState.wmppsScanForward:
                                parent.ScannedForward?.Invoke(parent, new EventArgs());
                                break;

                            case WMPLib.WMPPlayState.wmppsScanReverse:
                                parent.ScannedBackward?.Invoke(parent, new EventArgs());
                                break;

                            case WMPLib.WMPPlayState.wmppsMediaEnded:
                                parent.Finished?.Invoke(parent, new EventArgs());
                                break;
                        }
                    };
                    player.EndInit();
                    return player;
                },
                update: (items, control) =>
                {
                    var player = (MediaPlayer)control;
                    var wasVisible = player.Visible;
                    var url = items.Count == 1 ? FirstExistingPath(items[0]) : null;
                    player.Visible = url != null;
                    player.URL = url;
                    if (player.Visible && !wasVisible)
                    {
                        player.Dock = DockStyle.None;
                        player.Dock = DockStyle.Fill;
                        player.uiMode = "full";
                        player.enableContextMenu = false;
                        player.stretchToFit = true;
                        player.settings.mute = Settings.Default.DefaultMute;
                    }
                }),
        };

        private readonly Control[] displayInstances = new Control[MediaDisplays.Length];

        private SearchResultsVectors existingTags;

        private ImmutableList<SearchResult> previewItems = ImmutableList<SearchResult>.Empty;

        public PreviewControl(IMediaIndex index)
        {
            this.InitializeComponent();

            this.existingTags = new SearchResultsVectors(index);
            this.existingTags.Dock = DockStyle.Bottom;
            this.existingTags.Name = "existingTags";
            this.existingTags.TabIndex = 2;
            this.Controls.Add(this.existingTags);
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

        public static string FirstExistingPath(SearchResult searchResult) =>
            (from p in searchResult.Paths
             let path = PathEncoder.ExtendPath(p)
             where File.Exists(path)
             select path).FirstOrDefault();

        public static bool IsImage(SearchResult searchResult) =>
            searchResult != null && FileTypeHelper.IsImage(searchResult.FileType);

        protected override void OnVisibleChanged(EventArgs e)
        {
            this.UpdatePreview();
            base.OnVisibleChanged(e);
        }

        private void UpdatePreview()
        {
            var empty = ImmutableList<SearchResult>.Empty;
            var items = this.Visible ? this.previewItems : empty;
            this.existingTags.SearchResults = items;

            var selected = -1;
            for (var i = 0; i < PreviewControl.MediaDisplays.Length; i++)
            {
                var display = PreviewControl.MediaDisplays[i];
                var instance = this.displayInstances[i];
                if (selected < 0 && display.CanShow(items))
                {
                    if (instance == null)
                    {
                        this.Controls.Add(instance = this.displayInstances[i] = display.Create(items, this));
                        instance.TabIndex = 0;
                        instance.BringToFront();
                    }

                    selected = i;
                }
                else if (instance != null)
                {
                    display.Update(empty, instance);
                }
            }

            if (selected >= 0)
            {
                PreviewControl.MediaDisplays[selected].Update(items, this.displayInstances[selected]);
            }
        }

        private class Display
        {
            public Display(Predicate<IList<SearchResult>> canShow, Func<IList<SearchResult>, PreviewControl, Control> create, Action<IList<SearchResult>, Control> update)
            {
                this.CanShow = canShow ?? throw new ArgumentNullException(nameof(canShow));
                this.Create = create ?? throw new ArgumentNullException(nameof(create));
                this.Update = update ?? throw new ArgumentNullException(nameof(update));
            }

            public Predicate<IList<SearchResult>> CanShow { get; }

            public Func<IList<SearchResult>, PreviewControl, Control> Create { get; }

            public Action<IList<SearchResult>, Control> Update { get; }
        }
    }
}
