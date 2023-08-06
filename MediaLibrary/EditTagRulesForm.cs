// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using static System.Environment;

    public partial class EditTagRulesForm : Form
    {
        private readonly IMediaIndex index;
        private TextSearchForm textSearch;

        public EditTagRulesForm(IMediaIndex index)
        {
            this.index = index;
            this.InitializeComponent();
        }

        private TextBox GetRulesEditor(int documentIndex)
        {
            return GetRulesEditor(this.rulePages.TabPages[documentIndex]);
        }

        private static TextBox GetRulesEditor(TabPage p)
        {
            return (TextBox)p.Controls[0];
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
            newRules.AcceptsReturn = this.rules.AcceptsReturn;
            newRules.AcceptsTab = this.rules.AcceptsTab;
            newRules.HideSelection = false;
            newRules.Multiline = this.rules.Multiline;
            newRules.MaxLength = this.rules.MaxLength;
            newRules.WordWrap = this.rules.WordWrap;
            newRules.Font = this.rules.Font;
            newRules.ScrollBars = this.rules.ScrollBars;
            newRules.Size = this.rules.Size;
            newRules.Anchor = this.rules.Anchor;
            newRules.Dock = this.rules.Dock;
            newRules.Location = this.rules.Location;
            newRules.Text = rules;
            newRules.KeyDown += this.Rules_KeyDown;
            newPage.Controls.Add(newRules);
        }

        private void Rules_KeyDown(object sender, KeyEventArgs e)
        {
            void InitializeSearchForm()
            {
                if (this.textSearch == null)
                {
                    this.textSearch = new TextSearchForm();
                    this.textSearch.FindNext += (_, _) => FindNext(refocus: true);
                    this.textSearch.FindPrevious += (_, _) => FindPrevious(refocus: true);
                    this.FormClosed += (_, _) =>
                    {
                        this.textSearch.Dispose();
                    };
                }
            }

            void ShowSearchDialog()
            {
                InitializeSearchForm();

                if (!this.textSearch.Visible)
                {
                    var formBounds = ((Control)sender).GetFormRectangle();

                    var formPosition = new Point(
                        formBounds.X + (formBounds.Width - this.textSearch.Width),
                        formBounds.Y);

                    var screenPosition = new Point(
                        Math.Max(this.Location.X + formPosition.X, 0),
                        Math.Max(this.Location.Y + formPosition.Y, 0));

                    this.textSearch.Location = screenPosition;

                    this.textSearch.Show(this);
                }

                this.textSearch.Refocus();
            }

            (int documentIndex, int textIndex) GetCurrentLocation(bool reverse = false)
            {
                var documentIndex = this.rulePages.SelectedIndex;
                var editor = this.GetRulesEditor(documentIndex);
                var textIndex = reverse
                    ? Math.Min(editor.SelectionStart, editor.SelectionStart + editor.SelectionLength)
                    : Math.Max(editor.SelectionStart, editor.SelectionStart + editor.SelectionLength);
                return (documentIndex, textIndex);
            }

            void HighlightSearchResult(int documentIndex, int textIndex, int length, bool refocus)
            {
                this.rulePages.SelectedIndex = documentIndex;
                var editor = this.GetRulesEditor(documentIndex);
                editor.SelectionStart = textIndex; // Steals focus.
                editor.SelectionLength = length;
                editor.ScrollToCaret();

                if (refocus)
                {
                    this.textSearch.Refocus();
                }
            }

            void FindNext(bool refocus = false)
            {
                var search = this.textSearch?.Search;

                if (string.IsNullOrEmpty(search))
                {
                    ShowSearchDialog();
                    return;
                }

                var start = GetCurrentLocation();
                var state = start;

                (int documentIndex, int textIndex)? found = null;
                while (found == null)
                {
                    var aheadOfStart = state.documentIndex == start.documentIndex && state.textIndex < start.textIndex;
                    var nextIndex = this.GetRulesEditor(state.documentIndex).Text.IndexOf(search, state.textIndex, StringComparison.CurrentCultureIgnoreCase);
                    if (nextIndex == -1)
                    {
                        state.textIndex = 0;
                        state.documentIndex = (state.documentIndex + 1) % this.rulePages.TabCount;
                        if (aheadOfStart || state == start)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (!aheadOfStart || nextIndex < start.textIndex)
                        {
                            found = (state.documentIndex, nextIndex);
                        }

                        break;
                    }
                }

                if (found != null)
                {
                    HighlightSearchResult(found.Value.documentIndex, found.Value.textIndex, search.Length, refocus);
                }
            }

            void FindPrevious(bool refocus = false)
            {
                var search = this.textSearch?.Search;

                if (string.IsNullOrEmpty(search))
                {
                    ShowSearchDialog();
                    return;
                }

                var start = GetCurrentLocation(reverse: true);
                var state = start;

                (int documentIndex, int textIndex)? found = null;
                while (found == null)
                {
                    var aheadOfStart = state.documentIndex == start.documentIndex && state.textIndex > start.textIndex;
                    var nextIndex = state.textIndex < search.Length ? -1 : this.GetRulesEditor(state.documentIndex).Text.LastIndexOf(search, state.textIndex - (search.Length - 1), StringComparison.CurrentCultureIgnoreCase);
                    if (nextIndex == -1)
                    {
                        state.documentIndex = (state.documentIndex == 0 ? this.rulePages.TabCount : state.documentIndex) - 1;
                        state.textIndex = this.GetRulesEditor(state.documentIndex).Text.Length;
                        if (aheadOfStart || state == start)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (!aheadOfStart || nextIndex < start.textIndex)
                        {
                            found = (state.documentIndex, nextIndex);
                        }

                        break;
                    }
                }

                if (found != null)
                {
                    HighlightSearchResult(found.Value.documentIndex, found.Value.textIndex, search.Length, refocus);
                }
            }

            switch (e)
            {
                case { KeyCode: Keys.F3, Shift: false, Control: false, Alt: false }:
                    FindNext();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case { Shift: true, KeyCode: Keys.F3, Control: false, Alt: false }:
                    FindPrevious();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case { Control: true, KeyCode: Keys.F, Alt: false, Shift: false }:
                    ShowSearchDialog();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private async void ApplyButton_Click(object sender, System.EventArgs e)
        {
            await this.SaveChanges(swallowExceptions: true).ConfigureAwait(true);
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
                await this.SaveChanges(swallowExceptions: true).ConfigureAwait(true);
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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The contract of the `SaveChanges` function allows generic exception handling.")]
        private async void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                await this.SaveChanges(swallowExceptions: false).ConfigureAwait(true);
                this.DialogResult = DialogResult.OK;
                this.Hide();
            }
            catch
            {
                // Allow the user to rescue the state of the rules.
                // If the user wants to abort, a simple close or cancel action will work.
            }
        }

        private void RemoveCategoryMenuItem_Click(object sender, EventArgs e)
        {
            var tabIndex = this.tabContextMenu.Tag as int?;
            if (string.IsNullOrWhiteSpace(GetRulesEditor(tabIndex.Value).Text))
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

        private async Task SaveChanges(bool swallowExceptions)
        {
            var success = false;

            var ruleCategories = this.rulePages.TabPages.Cast<TabPage>().Select((p, i) => new RuleCategory(
                category: i == 0 ? string.Empty : p.Text,
                order: i,
                rules: GetRulesEditor(p).Text)).ToList();

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
                            if (swallowExceptions)
                            {
                                break;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                while (!success);
            }
            finally
            {
                this.Enabled = true;
            }
        }
    }
}
