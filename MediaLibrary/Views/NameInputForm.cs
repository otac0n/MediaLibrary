// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Views
{
    using System.Windows.Forms;

    public partial class NameInputForm : Form
    {
        public NameInputForm()
        {
            this.InitializeComponent();
        }

        public string SelectedName
        {
            get => this.name.Text;
            set => this.name.Text = value;
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
