// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class EditTagRulesForm : Form
    {
        private readonly MediaIndex index;

        public EditTagRulesForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private async void EditTagRulesForm_Load(object sender, EventArgs e)
        {
            this.rules.Text = await this.index.GetAllTagRules().ConfigureAwait(true);
            this.rules.Enabled = this.saveButton.Enabled = true;
        }

        private async void SaveButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                this.Enabled = false;
                await this.index.UpdateTagRules(this.rules.Text).ConfigureAwait(true);
            }
            finally
            {
                this.Enabled = true;
            }

            this.DialogResult = DialogResult.OK;
            this.Hide();
        }
    }
}
