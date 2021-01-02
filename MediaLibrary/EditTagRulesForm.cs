// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using static System.Environment;

    public partial class EditTagRulesForm : Form
    {
        private readonly MediaIndex index;

        public EditTagRulesForm(MediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private async void ApplyButton_Click(object sender, System.EventArgs e)
        {
            await this.SaveChanges().ConfigureAwait(true);
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private async void EditTagRulesForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S && !e.Alt && !e.Shift)
            {
                e.Handled = true;
                await this.SaveChanges().ConfigureAwait(true);
            }
        }

        private async void EditTagRulesForm_Load(object sender, EventArgs e)
        {
            this.rules.Text = await this.index.GetAllTagRules().ConfigureAwait(true);
            this.rules.Enabled = this.okButton.Enabled = this.applyButton.Enabled = true;
        }

        private async void OkButton_Click(object sender, EventArgs e)
        {
            await this.SaveChanges().ConfigureAwait(true);
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private async Task SaveChanges()
        {
            var success = false;
            var abort = false;

            try
            {
                this.Enabled = false;
                do
                {
                    try
                    {
                        await this.index.UpdateTagRules(this.rules.Text).ConfigureAwait(true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        var result = MessageBox.Show($"Error encountered: {ex.Message}{NewLine}{NewLine}", "Save Failed", MessageBoxButtons.RetryCancel);
                        if (result == DialogResult.Cancel)
                        {
                            abort = true;
                            throw;
                        }
                    }
                }
                while (!success && !abort);
            }
            finally
            {
                this.Enabled = true;
            }
        }
    }
}
