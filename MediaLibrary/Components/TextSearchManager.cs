namespace MediaLibrary.Components
{
    using System;
    using System.Windows.Forms;

    public class TextSearchManager : IDisposable
    {
        private readonly Form parent;
        private readonly Func<int> getDocumentCount;
        private readonly Func<int, string> getDocumentText;
        private readonly Func<(int documentIndex, int selectionStart, int selectionLength)> getCurrentSelection;
        private readonly Action<int, int, int> setCurrentSelection;
        private readonly Action<TextSearchForm> initializeForm;
        private TextSearchForm textSearch;

        public TextSearchManager(Form parent, Func<int> getDocumentCount, Func<int, string> getDocumentText, Func<(int documentIndex, int selectionStart, int selectionLength)> getCurrentSelection, Action<int, int, int> setCurrentSelection, Action<TextSearchForm> initializeForm)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.parent.KeyDown += this.HandleKeyDown;
            this.getDocumentCount = getDocumentCount;
            this.getDocumentText = getDocumentText;
            this.getCurrentSelection = getCurrentSelection;
            this.setCurrentSelection = setCurrentSelection;
            this.initializeForm = initializeForm;
        }

        private void InitializeSearchForm()
        {
            if (this.textSearch == null)
            {
                this.textSearch = new TextSearchForm();
                this.textSearch.FindNext += (_, _) => this.FindNext(refocus: true);
                this.textSearch.FindPrevious += (_, _) => this.FindPrevious(refocus: true);
            }
        }

        public void ShowSearchDialog()
        {
            this.InitializeSearchForm();

            if (!this.textSearch.Visible)
            {
                this.initializeForm(this.textSearch);
                this.textSearch.Show(this.parent);
            }

            this.textSearch.Refocus();
        }

        public bool HideSearchDialog()
        {
            if (this.textSearch?.Visible ?? false)
            {
                this.textSearch.Hide();
                return true;
            }

            return false;
        }

        public void HandleKeyDown(object sender, KeyEventArgs e)
        {
            switch (e)
            {
                case { KeyCode: Keys.F3, Shift: false, Control: false, Alt: false }:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    this.FindNext();
                    break;
                case { Shift: true, KeyCode: Keys.F3, Control: false, Alt: false }:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    this.FindPrevious();
                    break;
                case { Control: true, KeyCode: Keys.F, Alt: false, Shift: false }:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    this.ShowSearchDialog();
                    break;
            }
        }

        private (int documentIndex, int textIndex) GetCurrentLocation(Func<int, int, int> maxOrMin)
        {
            var (documentIndex, selectionStart, selectionLength) = this.getCurrentSelection();
            var textIndex = maxOrMin(selectionStart, selectionStart + selectionLength);
            return (documentIndex, textIndex);
        }

        private void HighlightSearchResult(int documentIndex, int textIndex, int length, bool refocus)
        {
            this.setCurrentSelection(documentIndex, textIndex, length);
            if (refocus)
            {
                this.textSearch.Refocus();
            }
        }

        public void FindNext(bool refocus = false)
        {
            var search = this.textSearch?.Search;

            if (string.IsNullOrEmpty(search))
            {
                this.ShowSearchDialog();
                return;
            }

            var start = this.GetCurrentLocation(Math.Max);
            var state = start;

            (int documentIndex, int textIndex)? found = null;
            while (found == null)
            {
                var aheadOfStart = state.documentIndex == start.documentIndex && state.textIndex < start.textIndex;
                var nextIndex = this.getDocumentText(state.documentIndex).IndexOf(search, state.textIndex, StringComparison.CurrentCultureIgnoreCase);
                if (nextIndex == -1)
                {
                    state.textIndex = 0;
                    state.documentIndex = (state.documentIndex + 1) % this.getDocumentCount();
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
                this.HighlightSearchResult(found.Value.documentIndex, found.Value.textIndex, search.Length, refocus);
            }
        }

        public void FindPrevious(bool refocus = false)
        {
            var search = this.textSearch?.Search;

            if (string.IsNullOrEmpty(search))
            {
                this.ShowSearchDialog();
                return;
            }

            var start = this.GetCurrentLocation(Math.Min);
            var state = start;

            (int documentIndex, int textIndex)? found = null;
            while (found == null)
            {
                var aheadOfStart = state.documentIndex == start.documentIndex && state.textIndex > start.textIndex;
                var nextIndex = state.textIndex < search.Length ? -1 : this.getDocumentText(state.documentIndex).LastIndexOf(search, state.textIndex - (search.Length - 1), StringComparison.CurrentCultureIgnoreCase);
                if (nextIndex == -1)
                {
                    state.documentIndex = (state.documentIndex == 0 ? this.getDocumentCount() : state.documentIndex) - 1;
                    state.textIndex = this.getDocumentText(state.documentIndex).Length;
                    if (aheadOfStart || state == start)
                    {
                        break;
                    }
                }
                else
                {
                    if (!aheadOfStart || nextIndex > start.textIndex)
                    {
                        found = (state.documentIndex, nextIndex);
                    }

                    break;
                }
            }

            if (found != null)
            {
                this.HighlightSearchResult(found.Value.documentIndex, found.Value.textIndex, search.Length, refocus);
            }
        }

        public void Dispose() => this.textSearch?.Dispose();
    }
}
