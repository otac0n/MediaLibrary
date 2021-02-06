// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using static System.Environment;

    public partial class EditTagRulesForm : Form
    {
        private readonly IMediaIndex index;

        public EditTagRulesForm(IMediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private static string GetRulesText(TabPage p)
        {
            return ((TextBox)p.Controls[0]).Text;
        }

        private void AddCategoryMenuItem_Click(object sender, EventArgs e)
        {
            var tabIndex = this.tabContextMenu.Tag as int?;
            using (var nameInputForm = new NameInputForm())
            {
                nameInputForm.Text = "New Category";
                if (nameInputForm.ShowDialog(this) == DialogResult.OK)
                {
                    // TODO: Append after the right-clicked tab.
                    this.AppendNewTab(nameInputForm.SelectedName); // TODO: Enusure it is unique, etc.
                }
            }
        }

        private void AppendNewTab(string category, string rules = "")
        {
            var newPage = new TabPage
            {
                Text = category,
            };

            this.rulePages.Controls.Add(newPage);

            var newRules = new TextBox();
            newPage.Controls.Add(newRules);
            newRules.AcceptsReturn = this.rules.AcceptsReturn;
            newRules.AcceptsTab = this.rules.AcceptsTab;
            newRules.Multiline = this.rules.Multiline;
            newRules.WordWrap = this.rules.WordWrap;
            newRules.Font = this.rules.Font;
            newRules.ScrollBars = this.rules.ScrollBars;
            newRules.Anchor = this.rules.Anchor;
            newRules.Location = this.rules.Location;
            newRules.Size = this.rules.Size;
            newRules.Text = rules;
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
            var ruleCategories = await this.index.GetAllRuleCategories().ConfigureAwait(true);

            var isDefaultLookup = ruleCategories.ToLookup(c => string.IsNullOrEmpty(c.Category));
            this.rules.Text = isDefaultLookup[true].FirstOrDefault()?.Rules ?? string.Empty;

            foreach (var c in isDefaultLookup[true].Skip(1).Concat(isDefaultLookup[false]).OrderBy(c => c.Order))
            {
                this.AppendNewTab(c.Category, c.Rules);
            }

            this.rulePages.Enabled = this.okButton.Enabled = this.applyButton.Enabled = true;
        }

        private async void OkButton_Click(object sender, EventArgs e)
        {
            await this.SaveChanges().ConfigureAwait(true);
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void RemoveCategoryMenuItem_Click(object sender, EventArgs e)
        {
            var tabIndex = this.tabContextMenu.Tag as int?;
            var tabPage = this.rulePages.TabPages[tabIndex.Value];
            if (string.IsNullOrWhiteSpace(GetRulesText(tabPage)))
            {
                this.rulePages.TabPages.RemoveAt(tabIndex.Value);
            }
            else
            {
                this.rulePages.SelectTab(tabIndex.Value);
                MessageBox.Show($"In order to avoid losing the rules in this category, the removal was blocked.{NewLine}{NewLine}To continue with the removal, move the rules to another category or delete them.", "Remove Category Blocked", MessageBoxButtons.OK);
            }
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            var tabIndex = this.tabContextMenu.Tag as int?;
            var tabPage = this.rulePages.TabPages[tabIndex.Value];
            using (var nameInputForm = new NameInputForm())
            {
                nameInputForm.Text = "Rename Category";
                nameInputForm.SelectedName = tabPage.Text;
                if (nameInputForm.ShowDialog(this) == DialogResult.OK)
                {
                    tabPage.Text = nameInputForm.SelectedName; // TODO: Enusure it is unique, etc.
                }
            }
        }

        private void RulePages_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                var tabIndex = this.rulePages.FindTabIndex(e.Location);
                this.tabContextMenu.Tag = tabIndex;
                this.removeCategoryMenuItem.Visible = this.renameMenuItem.Visible = tabIndex != 0;
                this.tabContextMenu.Show(Cursor.Position);
            }
        }

        private async Task SaveChanges()
        {
            var success = false;
            var abort = false;

            var ruleCategories = this.rulePages.TabPages.Cast<TabPage>().Select((p, i) => new RuleCategory(
                category: i == 0 ? string.Empty : p.Text,
                order: i,
                rules: GetRulesText(p))).ToList();

            try
            {
                this.Enabled = false;
                do
                {
                    try
                    {
                        await this.index.UpdateTagRules(ruleCategories).ConfigureAwait(true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        var result = MessageBox.Show($"Error encountered: {ex.Message}{NewLine}{NewLine}", "Save Failed", MessageBoxButtons.RetryCancel);
                        if (result == DialogResult.Cancel)
                        {
                            abort = true;
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
