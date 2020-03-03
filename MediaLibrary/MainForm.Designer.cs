namespace MediaLibrary
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findDuplicatesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.mainProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.searchBox = new System.Windows.Forms.ToolStripTextBox();
            this.listView = new System.Windows.Forms.ListView();
            this.nameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tagsColumns = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fileTypeImages = new System.Windows.Forms.ImageList(this.components);
            this.itemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.playButton = new System.Windows.Forms.ToolStripButton();
            this.playAllButton = new System.Windows.Forms.ToolStripButton();
            this.shuffleAllButton = new System.Windows.Forms.ToolStripButton();
            this.favoriteFilesDropDown = new System.Windows.Forms.ToolStripSplitButton();
            this.favoriteFilesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.favoriteAudioMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.favoriteImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.favoriteVideoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.starredDropDown = new System.Windows.Forms.ToolStripSplitButton();
            this.starredFilesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.starredAudioMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.starredImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.starredVideoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.homeButton = new System.Windows.Forms.ToolStripButton();
            this.audioDropDown = new System.Windows.Forms.ToolStripSplitButton();
            this.allAudioMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.audioFavoritesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.audioStarsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imagesDropDown = new System.Windows.Forms.ToolStripSplitButton();
            this.allImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.imageFavoritesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageStarsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoDropDown = new System.Windows.Forms.ToolStripSplitButton();
            this.allVideoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.videoFavoritesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoStarsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.detailsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.thumbnailsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addIndexedFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.favoriteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editTagsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSizeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mainMenu.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.itemContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.toolsMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.mainMenu.Size = new System.Drawing.Size(800, 24);
            this.mainMenu.TabIndex = 0;
            this.mainMenu.Text = "menuStrip";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addIndexedFolderToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // toolsMenuItem
            // 
            this.toolsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.findDuplicatesMenuItem});
            this.toolsMenuItem.Name = "toolsMenuItem";
            this.toolsMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsMenuItem.Text = "Tools";
            // 
            // findDuplicatesMenuItem
            // 
            this.findDuplicatesMenuItem.Image = global::MediaLibrary.Properties.Resources.common_file_stack;
            this.findDuplicatesMenuItem.Name = "findDuplicatesMenuItem";
            this.findDuplicatesMenuItem.Size = new System.Drawing.Size(180, 22);
            this.findDuplicatesMenuItem.Text = "Find &Duplicates...";
            this.findDuplicatesMenuItem.Click += new System.EventHandler(this.FindDuplicatesMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mainProgressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip.ShowItemToolTips = true;
            this.statusStrip.Size = new System.Drawing.Size(800, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip";
            // 
            // mainProgressBar
            // 
            this.mainProgressBar.Maximum = 1000;
            this.mainProgressBar.Name = "mainProgressBar";
            this.mainProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.playButton,
            this.playAllButton,
            this.shuffleAllButton,
            this.toolStripSeparator1,
            this.favoriteFilesDropDown,
            this.starredDropDown,
            this.toolStripSeparator2,
            this.homeButton,
            this.audioDropDown,
            this.imagesDropDown,
            this.videoDropDown,
            this.searchBox,
            this.viewButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 24);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // searchBox
            // 
            this.searchBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(200, 25);
            this.searchBox.TextChanged += new System.EventHandler(this.SearchBox_TextChangedAsync);
            // 
            // listView
            // 
            this.listView.AllowColumnReorder = true;
            this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.nameColumn,
            this.tagsColumns,
            this.fileSizeColumn});
            this.listView.FullRowSelect = true;
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(0, 49);
            this.listView.Margin = new System.Windows.Forms.Padding(0);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(800, 379);
            this.listView.SmallImageList = this.fileTypeImages;
            this.listView.TabIndex = 3;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ListView_MouseClick);
            this.listView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListView_DoubleClick);
            // 
            // nameColumn
            // 
            this.nameColumn.Text = "Name";
            // 
            // tagsColumns
            // 
            this.tagsColumns.Text = "Tags";
            // 
            // fileTypeImages
            // 
            this.fileTypeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("fileTypeImages.ImageStream")));
            this.fileTypeImages.TransparentColor = System.Drawing.Color.Transparent;
            this.fileTypeImages.Images.SetKeyName(0, "common-file");
            this.fileTypeImages.Images.SetKeyName(1, "audio-file");
            this.fileTypeImages.Images.SetKeyName(2, "image-file");
            this.fileTypeImages.Images.SetKeyName(3, "video-file");
            this.fileTypeImages.Images.SetKeyName(4, "audio-file-aac");
            this.fileTypeImages.Images.SetKeyName(5, "audio-file-aif");
            this.fileTypeImages.Images.SetKeyName(6, "audio-file-mid");
            this.fileTypeImages.Images.SetKeyName(7, "audio-file-mp3");
            this.fileTypeImages.Images.SetKeyName(8, "audio-file-wav");
            this.fileTypeImages.Images.SetKeyName(9, "image-file-bmp");
            this.fileTypeImages.Images.SetKeyName(10, "image-file-eps");
            this.fileTypeImages.Images.SetKeyName(11, "image-file-gif");
            this.fileTypeImages.Images.SetKeyName(12, "image-file-jpg");
            this.fileTypeImages.Images.SetKeyName(13, "image-file-png");
            this.fileTypeImages.Images.SetKeyName(14, "image-file-svg");
            this.fileTypeImages.Images.SetKeyName(15, "image-file-tiff");
            this.fileTypeImages.Images.SetKeyName(16, "video-file-avi");
            this.fileTypeImages.Images.SetKeyName(17, "video-file-flv");
            this.fileTypeImages.Images.SetKeyName(18, "video-file-m4v");
            this.fileTypeImages.Images.SetKeyName(19, "video-file-mov");
            this.fileTypeImages.Images.SetKeyName(20, "video-file-mp4");
            this.fileTypeImages.Images.SetKeyName(21, "video-file-mpg");
            this.fileTypeImages.Images.SetKeyName(22, "video-file-qt");
            // 
            // itemContextMenu
            // 
            this.itemContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.favoriteToolStripMenuItem,
            this.editTagsMenuItem});
            this.itemContextMenu.Name = "itemContextMenu";
            this.itemContextMenu.Size = new System.Drawing.Size(181, 70);
            // 
            // playButton
            // 
            this.playButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.playButton.Image = global::MediaLibrary.Properties.Resources.controls_play;
            this.playButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(23, 22);
            this.playButton.Text = "Play";
            // 
            // playAllButton
            // 
            this.playAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.playAllButton.Image = global::MediaLibrary.Properties.Resources.controls_forward;
            this.playAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.playAllButton.Name = "playAllButton";
            this.playAllButton.Size = new System.Drawing.Size(23, 22);
            this.playAllButton.Text = "Play all";
            // 
            // shuffleAllButton
            // 
            this.shuffleAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.shuffleAllButton.Image = global::MediaLibrary.Properties.Resources.button_shuffle;
            this.shuffleAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.shuffleAllButton.Name = "shuffleAllButton";
            this.shuffleAllButton.Size = new System.Drawing.Size(23, 22);
            this.shuffleAllButton.Text = "Shuffle all";
            // 
            // favoriteFilesDropDown
            // 
            this.favoriteFilesDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.favoriteFilesDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.favoriteFilesMenuItem,
            this.toolStripSeparator4,
            this.favoriteAudioMenuItem,
            this.favoriteImagesMenuItem,
            this.favoriteVideoMenuItem});
            this.favoriteFilesDropDown.Image = global::MediaLibrary.Properties.Resources.love_it;
            this.favoriteFilesDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.favoriteFilesDropDown.Name = "favoriteFilesDropDown";
            this.favoriteFilesDropDown.Size = new System.Drawing.Size(32, 22);
            this.favoriteFilesDropDown.Tag = "#favorite";
            this.favoriteFilesDropDown.Text = "Favorites";
            this.favoriteFilesDropDown.ButtonClick += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // favoriteFilesMenuItem
            // 
            this.favoriteFilesMenuItem.Image = global::MediaLibrary.Properties.Resources.common_file_heart;
            this.favoriteFilesMenuItem.Name = "favoriteFilesMenuItem";
            this.favoriteFilesMenuItem.Size = new System.Drawing.Size(112, 22);
            this.favoriteFilesMenuItem.Tag = "#favorite";
            this.favoriteFilesMenuItem.Text = "Files";
            this.favoriteFilesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(109, 6);
            // 
            // favoriteAudioMenuItem
            // 
            this.favoriteAudioMenuItem.Image = global::MediaLibrary.Properties.Resources.audio_file_heart;
            this.favoriteAudioMenuItem.Name = "favoriteAudioMenuItem";
            this.favoriteAudioMenuItem.Size = new System.Drawing.Size(112, 22);
            this.favoriteAudioMenuItem.Tag = "#favorite type:audio";
            this.favoriteAudioMenuItem.Text = "Audio";
            this.favoriteAudioMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // favoriteImagesMenuItem
            // 
            this.favoriteImagesMenuItem.Image = global::MediaLibrary.Properties.Resources.image_file_heart;
            this.favoriteImagesMenuItem.Name = "favoriteImagesMenuItem";
            this.favoriteImagesMenuItem.Size = new System.Drawing.Size(112, 22);
            this.favoriteImagesMenuItem.Tag = "#favorite type:image";
            this.favoriteImagesMenuItem.Text = "Images";
            this.favoriteImagesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // favoriteVideoMenuItem
            // 
            this.favoriteVideoMenuItem.Image = global::MediaLibrary.Properties.Resources.video_file_heart;
            this.favoriteVideoMenuItem.Name = "favoriteVideoMenuItem";
            this.favoriteVideoMenuItem.Size = new System.Drawing.Size(112, 22);
            this.favoriteVideoMenuItem.Tag = "#favorite type:video";
            this.favoriteVideoMenuItem.Text = "Video";
            this.favoriteVideoMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // starredDropDown
            // 
            this.starredDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.starredDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.starredFilesMenuItem,
            this.toolStripSeparator3,
            this.starredAudioMenuItem,
            this.starredImagesMenuItem,
            this.starredVideoMenuItem});
            this.starredDropDown.Image = global::MediaLibrary.Properties.Resources.rating_star;
            this.starredDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.starredDropDown.Name = "starredDropDown";
            this.starredDropDown.Size = new System.Drawing.Size(32, 22);
            this.starredDropDown.Tag = "stars:>=3";
            this.starredDropDown.Text = "Starred";
            this.starredDropDown.ButtonClick += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // starredFilesMenuItem
            // 
            this.starredFilesMenuItem.Image = global::MediaLibrary.Properties.Resources.common_file_star;
            this.starredFilesMenuItem.Name = "starredFilesMenuItem";
            this.starredFilesMenuItem.Size = new System.Drawing.Size(112, 22);
            this.starredFilesMenuItem.Tag = "stars:>=3";
            this.starredFilesMenuItem.Text = "Files";
            this.starredFilesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(109, 6);
            // 
            // starredAudioMenuItem
            // 
            this.starredAudioMenuItem.Image = global::MediaLibrary.Properties.Resources.audio_file_star;
            this.starredAudioMenuItem.Name = "starredAudioMenuItem";
            this.starredAudioMenuItem.Size = new System.Drawing.Size(112, 22);
            this.starredAudioMenuItem.Tag = "stars:>=3 type:audio";
            this.starredAudioMenuItem.Text = "Audio";
            this.starredAudioMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // starredImagesMenuItem
            // 
            this.starredImagesMenuItem.Image = global::MediaLibrary.Properties.Resources.image_file_star;
            this.starredImagesMenuItem.Name = "starredImagesMenuItem";
            this.starredImagesMenuItem.Size = new System.Drawing.Size(112, 22);
            this.starredImagesMenuItem.Tag = "stars:>=3 type:image";
            this.starredImagesMenuItem.Text = "Images";
            this.starredImagesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // starredVideoMenuItem
            // 
            this.starredVideoMenuItem.Image = global::MediaLibrary.Properties.Resources.video_file_star;
            this.starredVideoMenuItem.Name = "starredVideoMenuItem";
            this.starredVideoMenuItem.Size = new System.Drawing.Size(112, 22);
            this.starredVideoMenuItem.Tag = "stars:>=3 type:video";
            this.starredVideoMenuItem.Text = "Video";
            this.starredVideoMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // homeButton
            // 
            this.homeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.homeButton.Image = global::MediaLibrary.Properties.Resources.house_2;
            this.homeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.homeButton.Name = "homeButton";
            this.homeButton.Size = new System.Drawing.Size(23, 22);
            this.homeButton.Text = "Home";
            this.homeButton.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // audioDropDown
            // 
            this.audioDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.audioDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allAudioMenuItem,
            this.toolStripSeparator5,
            this.audioFavoritesMenuItem,
            this.audioStarsMenuItem});
            this.audioDropDown.Image = global::MediaLibrary.Properties.Resources.music_note_1;
            this.audioDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.audioDropDown.Name = "audioDropDown";
            this.audioDropDown.Size = new System.Drawing.Size(32, 22);
            this.audioDropDown.Tag = "type:audio";
            this.audioDropDown.Text = "Audio";
            this.audioDropDown.ButtonClick += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // allAudioMenuItem
            // 
            this.allAudioMenuItem.Image = global::MediaLibrary.Properties.Resources.audio_file_home;
            this.allAudioMenuItem.Name = "allAudioMenuItem";
            this.allAudioMenuItem.Size = new System.Drawing.Size(151, 22);
            this.allAudioMenuItem.Tag = "type:audio";
            this.allAudioMenuItem.Text = "All Audio";
            this.allAudioMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(148, 6);
            // 
            // audioFavoritesMenuItem
            // 
            this.audioFavoritesMenuItem.Image = global::MediaLibrary.Properties.Resources.audio_file_heart;
            this.audioFavoritesMenuItem.Name = "audioFavoritesMenuItem";
            this.audioFavoritesMenuItem.Size = new System.Drawing.Size(151, 22);
            this.audioFavoritesMenuItem.Tag = "type:audio #favorite";
            this.audioFavoritesMenuItem.Text = "Favorite Audio";
            this.audioFavoritesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // audioStarsMenuItem
            // 
            this.audioStarsMenuItem.Image = global::MediaLibrary.Properties.Resources.audio_file_star;
            this.audioStarsMenuItem.Name = "audioStarsMenuItem";
            this.audioStarsMenuItem.Size = new System.Drawing.Size(151, 22);
            this.audioStarsMenuItem.Tag = "type:audio stars:>=3";
            this.audioStarsMenuItem.Text = "Starred Audio";
            this.audioStarsMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // imagesDropDown
            // 
            this.imagesDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.imagesDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allImagesMenuItem,
            this.toolStripSeparator6,
            this.imageFavoritesMenuItem,
            this.imageStarsMenuItem});
            this.imagesDropDown.Image = global::MediaLibrary.Properties.Resources.picture_landscape;
            this.imagesDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.imagesDropDown.Name = "imagesDropDown";
            this.imagesDropDown.Size = new System.Drawing.Size(32, 22);
            this.imagesDropDown.Tag = "type:image";
            this.imagesDropDown.Text = "Images";
            this.imagesDropDown.ButtonClick += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // allImagesMenuItem
            // 
            this.allImagesMenuItem.Image = global::MediaLibrary.Properties.Resources.image_file_home;
            this.allImagesMenuItem.Name = "allImagesMenuItem";
            this.allImagesMenuItem.Size = new System.Drawing.Size(157, 22);
            this.allImagesMenuItem.Tag = "type:image";
            this.allImagesMenuItem.Text = "All Images";
            this.allImagesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(154, 6);
            // 
            // imageFavoritesMenuItem
            // 
            this.imageFavoritesMenuItem.Image = global::MediaLibrary.Properties.Resources.image_file_heart;
            this.imageFavoritesMenuItem.Name = "imageFavoritesMenuItem";
            this.imageFavoritesMenuItem.Size = new System.Drawing.Size(157, 22);
            this.imageFavoritesMenuItem.Tag = "type:image #favorite";
            this.imageFavoritesMenuItem.Text = "Favorite Images";
            this.imageFavoritesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // imageStarsMenuItem
            // 
            this.imageStarsMenuItem.Image = global::MediaLibrary.Properties.Resources.image_file_star;
            this.imageStarsMenuItem.Name = "imageStarsMenuItem";
            this.imageStarsMenuItem.Size = new System.Drawing.Size(157, 22);
            this.imageStarsMenuItem.Tag = "type:image stars:>=3";
            this.imageStarsMenuItem.Text = "Starred Images";
            this.imageStarsMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // videoDropDown
            // 
            this.videoDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.videoDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allVideoMenuItem,
            this.toolStripSeparator7,
            this.videoFavoritesMenuItem,
            this.videoStarsMenuItem});
            this.videoDropDown.Image = global::MediaLibrary.Properties.Resources.video_player_movie;
            this.videoDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.videoDropDown.Name = "videoDropDown";
            this.videoDropDown.Size = new System.Drawing.Size(32, 22);
            this.videoDropDown.Tag = "type:video";
            this.videoDropDown.Text = "Videos";
            this.videoDropDown.ButtonClick += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // allVideoMenuItem
            // 
            this.allVideoMenuItem.Image = global::MediaLibrary.Properties.Resources.video_file_home;
            this.allVideoMenuItem.Name = "allVideoMenuItem";
            this.allVideoMenuItem.Size = new System.Drawing.Size(149, 22);
            this.allVideoMenuItem.Tag = "type:video";
            this.allVideoMenuItem.Text = "All Video";
            this.allVideoMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(146, 6);
            // 
            // videoFavoritesMenuItem
            // 
            this.videoFavoritesMenuItem.Image = global::MediaLibrary.Properties.Resources.video_file_heart;
            this.videoFavoritesMenuItem.Name = "videoFavoritesMenuItem";
            this.videoFavoritesMenuItem.Size = new System.Drawing.Size(149, 22);
            this.videoFavoritesMenuItem.Tag = "type:video #favorite";
            this.videoFavoritesMenuItem.Text = "Favorite Video";
            this.videoFavoritesMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // videoStarsMenuItem
            // 
            this.videoStarsMenuItem.Image = global::MediaLibrary.Properties.Resources.video_file_star;
            this.videoStarsMenuItem.Name = "videoStarsMenuItem";
            this.videoStarsMenuItem.Size = new System.Drawing.Size(149, 22);
            this.videoStarsMenuItem.Tag = "type:video stars:>=3";
            this.videoStarsMenuItem.Text = "Starred Video";
            this.videoStarsMenuItem.Click += new System.EventHandler(this.SearchBookmark_Click);
            // 
            // viewButton
            // 
            this.viewButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.viewButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.detailsMenuItem,
            this.thumbnailsMenuItem});
            this.viewButton.Image = global::MediaLibrary.Properties.Resources.view_1;
            this.viewButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.viewButton.Name = "viewButton";
            this.viewButton.Size = new System.Drawing.Size(29, 22);
            this.viewButton.Text = "toolStripDropDownButton1";
            // 
            // detailsMenuItem
            // 
            this.detailsMenuItem.Checked = true;
            this.detailsMenuItem.CheckOnClick = true;
            this.detailsMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.detailsMenuItem.Image = global::MediaLibrary.Properties.Resources.list_bullets_1;
            this.detailsMenuItem.Name = "detailsMenuItem";
            this.detailsMenuItem.Size = new System.Drawing.Size(137, 22);
            this.detailsMenuItem.Text = "Details";
            this.detailsMenuItem.Click += new System.EventHandler(this.DetailsMenuItem_Click);
            // 
            // thumbnailsMenuItem
            // 
            this.thumbnailsMenuItem.CheckOnClick = true;
            this.thumbnailsMenuItem.Image = global::MediaLibrary.Properties.Resources.picture_landscape;
            this.thumbnailsMenuItem.Name = "thumbnailsMenuItem";
            this.thumbnailsMenuItem.Size = new System.Drawing.Size(137, 22);
            this.thumbnailsMenuItem.Text = "Thumbnails";
            this.thumbnailsMenuItem.Click += new System.EventHandler(this.ThumbnailsMenuItem_Click);
            // 
            // addIndexedFolderToolStripMenuItem
            // 
            this.addIndexedFolderToolStripMenuItem.Image = global::MediaLibrary.Properties.Resources.folder_add;
            this.addIndexedFolderToolStripMenuItem.Name = "addIndexedFolderToolStripMenuItem";
            this.addIndexedFolderToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.addIndexedFolderToolStripMenuItem.Text = "Add &Indexed Folder...";
            this.addIndexedFolderToolStripMenuItem.Click += new System.EventHandler(this.AddIndexedFolderToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Image = global::MediaLibrary.Properties.Resources.information_circle;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.aboutToolStripMenuItem.Text = "Ab&out...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // favoriteToolStripMenuItem
            // 
            this.favoriteToolStripMenuItem.Image = global::MediaLibrary.Properties.Resources.love_it;
            this.favoriteToolStripMenuItem.Name = "favoriteToolStripMenuItem";
            this.favoriteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.favoriteToolStripMenuItem.Text = "&Favorite";
            this.favoriteToolStripMenuItem.Click += new System.EventHandler(this.FavoriteToolStripMenuItem_Click);
            // 
            // editTagsMenuItem
            // 
            this.editTagsMenuItem.Image = global::MediaLibrary.Properties.Resources.tags_edit;
            this.editTagsMenuItem.Name = "editTagsMenuItem";
            this.editTagsMenuItem.Size = new System.Drawing.Size(180, 22);
            this.editTagsMenuItem.Text = "Edit &Tags...";
            this.editTagsMenuItem.Click += new System.EventHandler(this.AddTagsToolStripMenuItem_Click);
            // 
            // fileSizeColumn
            // 
            this.fileSizeColumn.Text = "File Size";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.mainMenu);
            this.MainMenuStrip = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.itemContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addIndexedFolderToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripProgressBar mainProgressBar;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton playButton;
        private System.Windows.Forms.ToolStripButton playAllButton;
        private System.Windows.Forms.ToolStripButton shuffleAllButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton homeButton;
        private System.Windows.Forms.ToolStripTextBox searchBox;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ImageList fileTypeImages;
        private System.Windows.Forms.ColumnHeader nameColumn;
        private System.Windows.Forms.ToolStripSplitButton favoriteFilesDropDown;
        private System.Windows.Forms.ToolStripMenuItem favoriteFilesMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem favoriteAudioMenuItem;
        private System.Windows.Forms.ToolStripMenuItem favoriteImagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem favoriteVideoMenuItem;
        private System.Windows.Forms.ToolStripSplitButton starredDropDown;
        private System.Windows.Forms.ToolStripMenuItem starredFilesMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem starredAudioMenuItem;
        private System.Windows.Forms.ToolStripMenuItem starredImagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem starredVideoMenuItem;
        private System.Windows.Forms.ToolStripSplitButton audioDropDown;
        private System.Windows.Forms.ToolStripMenuItem allAudioMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem audioFavoritesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem audioStarsMenuItem;
        private System.Windows.Forms.ToolStripSplitButton imagesDropDown;
        private System.Windows.Forms.ToolStripMenuItem allImagesMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem imageFavoritesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageStarsMenuItem;
        private System.Windows.Forms.ToolStripSplitButton videoDropDown;
        private System.Windows.Forms.ToolStripMenuItem allVideoMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem videoFavoritesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem videoStarsMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton viewButton;
        private System.Windows.Forms.ToolStripMenuItem detailsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem thumbnailsMenuItem;
        private System.Windows.Forms.ColumnHeader tagsColumns;
        private System.Windows.Forms.ContextMenuStrip itemContextMenu;
        private System.Windows.Forms.ToolStripMenuItem favoriteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editTagsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findDuplicatesMenuItem;
        private System.Windows.Forms.ColumnHeader fileSizeColumn;
    }
}
