// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class AddPeopleForm : Form
    {
        private readonly MediaIndex index;
        private readonly Dictionary<int, PersonControl> personControls = new Dictionary<int, PersonControl>();
        private readonly IList<SearchResult> searchResults;

        public AddPeopleForm(MediaIndex index, IList<SearchResult> searchResults)
        {
            this.InitializeComponent();
            this.index = index;
            this.searchResults = searchResults;
            this.PopulateExistingPeople();
            this.PopulatePeopleCombo();
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            var name = this.personSearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!(this.personSearchBox.SelectedPerson is Person person))
            {
                this.personSearchBox.Text = string.Empty;
                this.personSearchBox.Focus();

                person = await this.index.AddPerson(name).ConfigureAwait(true);
            }
            else
            {
                this.personSearchBox.Text = string.Empty;
                this.personSearchBox.Focus();
            }

            if (this.personControls.TryGetValue(person.PersonId, out var personControl))
            {
                personControl.Indeterminate = false;
                this.existingPeople.ScrollControlIntoView(personControl);
            }
            else
            {
                personControl = this.AddPersonControl(person, indeterminate: false);
                this.existingPeople.ScrollControlIntoView(personControl);
            }

            foreach (var searchResult in this.searchResults)
            {
                await this.index.AddHashPerson(new HashPerson(searchResult.Hash, person.PersonId)).ConfigureAwait(false);
            }
        }

        private void AddPeopleForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private PersonControl AddPersonControl(Person person, bool indeterminate)
        {
            var personControl = new PersonControl { Person = person, Indeterminate = indeterminate };
            personControl.DeleteClick += this.PersonControl_DeleteClick;
            this.existingPeople.Controls.Add(this.personControls[person.PersonId] = personControl);
            return personControl;
        }

        private async void PersonControl_DeleteClick(object sender, EventArgs e)
        {
            var personControl = (PersonControl)sender;
            personControl.DeleteClick -= this.PersonControl_DeleteClick;
            this.existingPeople.Controls.Remove(personControl);

            foreach (var searchResult in this.searchResults)
            {
                var personId = personControl.Person.PersonId;
                await this.index.RemoveHashPerson(new HashPerson(searchResult.Hash, personId)).ConfigureAwait(false);
            }
        }

        private void PopulateExistingPeople()
        {
            var people = new Dictionary<int, Person>();
            var personCounts = new Dictionary<int, int>();
            foreach (var person in this.searchResults.SelectMany(r => r.People))
            {
                people[person.PersonId] = person;
                personCounts[person.PersonId] = personCounts.TryGetValue(person.PersonId, out var count) ? count + 1 : 1;
            }

            foreach (var person in personCounts)
            {
                this.AddPersonControl(people[person.Key], person.Value != this.searchResults.Count);
            }
        }

        private async void PopulatePeopleCombo()
        {
            var people = await this.index.GetAllPeople().ConfigureAwait(true);
            var text = this.personSearchBox.Text;
            this.personSearchBox.People = people;
            this.personSearchBox.Text = text;
        }
    }
}
