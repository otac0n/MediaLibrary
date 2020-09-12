// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class EditSavedSearchForm : Form
    {
        private SavedSearch savedSearch;

        public EditSavedSearchForm(SavedSearch savedSearch)
        {
            this.InitializeComponent();
            this.SavedSearch = savedSearch;
        }

        public SavedSearch SavedSearch
        {
            get
            {
                if (this.savedSearch.Name != this.name.Text || this.savedSearch.Query != this.query.Text)
                {
                    this.savedSearch = new SavedSearch(this.savedSearch.SearchId, this.name.Text, this.query.Text);
                }

                return this.savedSearch;
            }

            set
            {
                this.savedSearch = value ?? throw new ArgumentNullException(nameof(value));
                this.name.Text = value.Name;
                this.query.Text = value.Query;
            }
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private void FinishButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }
    }
}
