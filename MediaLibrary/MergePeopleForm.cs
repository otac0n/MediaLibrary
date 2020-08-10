// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class MergePeopleForm : Form
    {
        private readonly MediaIndex index;
        private List<Person> people;

        public MergePeopleForm(MediaIndex index)
        {
            this.InitializeComponent();
            this.index = index;
            this.PopulatePeopleSearchBox();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void EditPersonForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Hide();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var personA = this.personASearchBox.SelectedPerson;
            var personB = this.personBSearchBox.SelectedPerson;

            var result = MessageBox.Show($"This will merge {personA.Name} (ID: {personA.PersonId}) and {personB.Name} (ID: {personB.PersonId}). This is a destructive operation and cannot be undone. Are you sure you want to merge these people?", "Are you sure?", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.index.MergePeople(personA.PersonId, personB.PersonId);
                this.Hide();
            }
        }

        private void PersonSearchBox_SelectedPersonChanged(object sender, EventArgs e)
        {
            this.RefreshView();
        }

        private async void PopulatePeopleSearchBox()
        {
            this.people = await this.index.GetAllPeople().ConfigureAwait(true);

            var text = this.personASearchBox.Text;
            this.personASearchBox.People = this.people;
            this.personASearchBox.Text = text;

            text = this.personBSearchBox.Text;
            this.personBSearchBox.People = this.people;
            this.personBSearchBox.Text = text;
        }

        private void RefreshView()
        {
            var personA = this.personASearchBox.SelectedPerson;
            var personB = this.personBSearchBox.SelectedPerson;
            this.okButton.Enabled = personA != null && personB != null && personA.PersonId != personB.PersonId;
        }
    }
}
