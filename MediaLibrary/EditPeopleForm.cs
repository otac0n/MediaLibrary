// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class EditPeopleForm : Form
    {
        private readonly MediaIndex index;
        private List<Person> people;
        private int update;

        public EditPeopleForm(MediaIndex index)
        {
            this.InitializeComponent();
            this.index = index;
            this.PopulatePeopleSearchBox();
        }

        public Person SelectedPerson { get; private set; }

        public List<Alias> SelectedPersonAliases { get; private set; }

        private static bool IsGeneric(Alias a) => a.Site == null;

        private async void AddUsernameButton_Click(object sender, EventArgs e)
        {
            var site = this.siteTextBox.Text;
            var person = this.SelectedPerson;
            var username = this.usernameTextBox.Text;

            if (!string.IsNullOrEmpty(site) && !string.IsNullOrEmpty(username))
            {
                try
                {
                    this.EnterUpdate();

                    this.siteTextBox.Text = string.Empty;
                    this.usernameTextBox.Text = string.Empty;

                    var needsRefresh = false;
                    if (!this.SelectedPersonAliases.Any(a => a.Site == site && a.Name == username))
                    {
                        var alias = new Alias(person.PersonId, site, username);
                        await this.index.AddAlias(alias).ConfigureAwait(true);
                        this.SelectedPersonAliases.Add(alias);
                        needsRefresh = true;
                    }

                    if (needsRefresh)
                    {
                        this.RefreshView();
                    }
                }
                finally
                {
                    this.ExitUpdate();
                }
            }
        }

        private async void AliasControl_DeleteClick(object sender, EventArgs e)
        {
            try
            {
                this.EnterUpdate();

                var personControl = (PersonControl)sender;
                var alias = (Alias)personControl.Tag;
                await this.index.RemoveAlias(alias).ConfigureAwait(true);
                this.SelectedPersonAliases.Remove(alias);
                this.RefreshView();
            }
            finally
            {
                this.ExitUpdate();
            }
        }

        private async void AliasTextBox_Validated(object sender, EventArgs e)
        {
            if (this.editorTablePanel.Enabled)
            {
                var textBox = (TextBox)sender;
                var newName = textBox.Text;
                var person = this.SelectedPerson;
                var alias = (Alias)textBox.Tag;

                try
                {
                    this.EnterUpdate();

                    var needsRefresh = false;
                    if (alias == null)
                    {
                        if (!string.IsNullOrEmpty(newName))
                        {
                            textBox.Tag = alias = new Alias(person.PersonId, null, newName);

                            if (!this.SelectedPersonAliases.Any(a => IsGeneric(a) && a.Name == newName))
                            {
                                await this.index.AddAlias(alias).ConfigureAwait(true);
                            }

                            this.SelectedPersonAliases.Add(alias);
                            needsRefresh = true;
                        }
                    }
                    else
                    {
                        if (newName != alias.Name)
                        {
                            var oldAlias = alias;
                            var oldIndex = this.SelectedPersonAliases.IndexOf(oldAlias);
                            this.SelectedPersonAliases.RemoveAt(oldIndex);

                            if (!string.IsNullOrEmpty(newName))
                            {
                                textBox.Tag = alias = new Alias(person.PersonId, null, newName);

                                if (!this.SelectedPersonAliases.Any(a => IsGeneric(a) && a.Name == newName))
                                {
                                    await this.index.AddAlias(alias).ConfigureAwait(true);
                                }

                                this.SelectedPersonAliases.Insert(oldIndex, alias);
                                needsRefresh = true;
                            }

                            if (!this.SelectedPersonAliases.Any(a => IsGeneric(a) && a.Name == newName))
                            {
                                await this.index.RemoveAlias(oldAlias).ConfigureAwait(true);
                                needsRefresh = true;
                            }

                            this.RefreshView();
                        }
                    }

                    if (needsRefresh)
                    {
                        this.RefreshView();
                    }
                }
                finally
                {
                    this.ExitUpdate();
                }
            }
        }

        private void EditPersonForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void EnterUpdate()
        {
            Interlocked.Increment(ref this.update);
            this.editorTablePanel.Enabled = false;
        }

        private void ExitUpdate()
        {
            if (Interlocked.Decrement(ref this.update) == 0)
            {
                this.editorTablePanel.Enabled = true;
            }
        }

        private async void NameTextBox_Validated(object sender, EventArgs e)
        {
            if (this.editorTablePanel.Enabled)
            {
                var person = this.SelectedPerson;
                var aliases = this.SelectedPersonAliases;

                var previousName = person.Name;
                var newName = this.nameTextBox.Text;
                if (newName != previousName)
                {
                    var needsRefresh = false;
                    this.EnterUpdate();
                    try
                    {
                        if (!aliases.Any(a => IsGeneric(a) && a.Name == previousName))
                        {
                            var alias = new Alias(person.PersonId, null, previousName);
                            await this.index.AddAlias(alias).ConfigureAwait(true);
                            this.SelectedPersonAliases.Add(alias);
                            needsRefresh = true;
                        }

                        person.Name = newName;
                        await this.index.UpdatePerson(person).ConfigureAwait(true);
                        this.SelectedPerson = person;

                        if (needsRefresh)
                        {
                            this.RefreshView();
                        }
                    }
                    finally
                    {
                        this.ExitUpdate();
                    }
                }
            }
        }

        private async void PersonSearchBox_SelectedPersonChanged(object sender, EventArgs e)
        {
            this.editorTablePanel.Enabled = false;
            if (this.personSearchBox.SelectedPerson is Person person)
            {
                this.SelectedPerson = await this.index.GetPersonById(person.PersonId).ConfigureAwait(true);
                this.SelectedPersonAliases = await this.index.GetAliases(person.PersonId).ConfigureAwait(true);
                this.siteTextBox.Text = string.Empty;
                this.usernameTextBox.Text = string.Empty;
                this.RefreshView();
            }
        }

        private async void PopulatePeopleSearchBox()
        {
            this.people = await this.index.GetAllPeople().ConfigureAwait(true);
            var text = this.personSearchBox.Text;
            this.personSearchBox.People = this.people;
            this.personSearchBox.Text = text;
        }

        private void RefreshView()
        {
            const int TypeGeneric = 0;
            const int TypeSiteSpecific = 1;
            var aliasTypeLookup = this.SelectedPersonAliases.ToLookup(a => IsGeneric(a) ? TypeGeneric : TypeSiteSpecific);

            this.EnterUpdate();
            try
            {
                this.nameTextBox.Text = this.SelectedPerson.Name;
                this.usernamesFlowPanel.Controls.OfType<PersonControl>();

                var akaAliases = aliasTypeLookup[TypeGeneric].ToList();
                this.aliasesTablePanel.RowCount = Math.Max(this.aliasesTablePanel.RowCount, akaAliases.Count + 1);

                for (var i = 0; i < this.aliasesTablePanel.RowCount; i++)
                {
                    var label = this.aliasesTablePanel.GetControlFromPosition(0, i);
                    var textBox = this.aliasesTablePanel.GetControlFromPosition(1, i);
                    if (label == null)
                    {
                        label = new Label { Text = "aka" };
                        this.aliasesTablePanel.Controls.Add(label, 0, i);
                    }

                    if (textBox == null && i <= akaAliases.Count)
                    {
                        textBox = new TextBox { Dock = DockStyle.Fill };
                        textBox.Validated += this.AliasTextBox_Validated;
                        this.aliasesTablePanel.Controls.Add(textBox, 1, i);
                    }

                    if (i < akaAliases.Count)
                    {
                        var alias = akaAliases[i];
                        textBox.Text = alias.Name;
                        textBox.Tag = alias;
                    }
                    else if (i == akaAliases.Count)
                    {
                        textBox.Text = string.Empty;
                        textBox.Tag = null;
                    }
                    else
                    {
                        if (label != null)
                        {
                            this.aliasesTablePanel.Controls.Remove(label);
                            label.Dispose();
                        }

                        if (textBox != null)
                        {
                            textBox.Validated -= this.AliasTextBox_Validated;
                            this.aliasesTablePanel.Controls.Remove(textBox);
                            textBox.Dispose();
                        }
                    }
                }

                this.aliasesTablePanel.RowCount = akaAliases.Count + 1;

                this.usernamesFlowPanel.Controls.Clear();
                foreach (var alias in aliasTypeLookup[TypeSiteSpecific])
                {
                    var aliasControl = new PersonControl
                    {
                        Alias = alias,
                        AllowDelete = true,
                        Tag = alias,
                    };

                    aliasControl.DeleteClick += this.AliasControl_DeleteClick;
                    this.usernamesFlowPanel.Controls.Add(aliasControl);
                }
            }
            finally
            {
                this.ExitUpdate();
            }
        }
    }
}
