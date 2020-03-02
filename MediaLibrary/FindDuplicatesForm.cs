// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System.Linq;
    using System.Windows.Forms;
    using ByteSizeLib;
    using MediaLibrary.Storage;

    public partial class FindDuplicatesForm : Form
    {
        private readonly MediaIndex index;

        public FindDuplicatesForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private async void FindDuplicatesForm_Load(object sender, System.EventArgs e)
        {
            var results = await this.index.SearchIndex("copies>1").ConfigureAwait(true);

            this.duplicatesList.BeginUpdate();

            foreach (var result in results.OrderByDescending(r => r.FileSize * (r.Paths.Length - 1)))
            {
                var group = new ListViewGroup($"{result.Hash} ({ByteSize.FromBytes(result.FileSize)} × {result.Paths.Length})");
                this.duplicatesList.Groups.Add(group);
                foreach (var path in result.Paths)
                {
                    var item = new ListViewItem(path, group)
                    {
                        Checked = true,
                    };
                    this.duplicatesList.Items.Add(item);
                }
            }

            this.duplicatesList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.duplicatesList.Enabled = this.okButton.Enabled = true;
            this.duplicatesList.EndUpdate();
        }
    }
}
