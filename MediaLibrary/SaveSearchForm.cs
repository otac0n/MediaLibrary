// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System.Windows.Forms;

    public partial class SaveSearchForm : Form
    {
        public SaveSearchForm()
        {
            this.InitializeComponent();
        }

        public string SelectedName => this.name.Text;

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
