// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class PersonSearchBox : ComboBox
    {
        private static readonly StringComparer Comparer = StringComparer.CurrentCultureIgnoreCase;
        private static readonly StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;
        private static readonly Regex NoMatch = new Regex("(?!)");

        private Font highlightFont;

        private ImmutableList<Person> people = ImmutableList<Person>.Empty;

        private bool selEndCancel;
        private int selEndIndex;
        private int selEndSelectionLength;
        private int selEndSelectionStart;
        private string selEndText;

        private HashSet<string> terms = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public PersonSearchBox()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.highlightFont = new Font(this.Font, FontStyle.Bold);
        }

        public event EventHandler<EventArgs> SelectedPersonChanged;

        private enum ComboBoxMessages
        {
            CB_GETEDITSEL = 0x0140,
            CB_LIMITTEXT = 0x0141,
            CB_SETEDITSEL = 0x0142,
            CB_ADDSTRING = 0x0143,
            CB_DELETESTRING = 0x0144,
            CB_DIR = 0x0145,
            CB_GETCOUNT = 0x0146,
            CB_GETCURSEL = 0x0147,
            CB_GETLBTEXT = 0x0148,
            CB_GETLBTEXTLEN = 0x0149,
            CB_INSERTSTRING = 0x014A,
            CB_RESETCONTENT = 0x014B,
            CB_FINDSTRING = 0x014C,
            CB_SELECTSTRING = 0x014D,
            CB_SETCURSEL = 0x014E,
            CB_SHOWDROPDOWN = 0x014F,
            CB_GETITEMDATA = 0x0150,
            CB_SETITEMDATA = 0x0151,
            CB_GETDROPPEDCONTROLRECT = 0x0152,
            CB_SETITEMHEIGHT = 0x0153,
            CB_GETITEMHEIGHT = 0x0154,
            CB_SETEXTENDEDUI = 0x0155,
            CB_GETEXTENDEDUI = 0x0156,
            CB_GETDROPPEDSTATE = 0x0157,
            CB_FINDSTRINGEXACT = 0x0158,
            CB_SETLOCALE = 0x0159,
            CB_GETLOCALE = 0x015A,
            CB_GETTOPINDEX = 0x015b,
            CB_SETTOPINDEX = 0x015c,
            CB_GETHORIZONTALEXTENT = 0x015d,
            CB_SETHORIZONTALEXTENT = 0x015e,
            CB_GETDROPPEDWIDTH = 0x015f,
            CB_SETDROPPEDWIDTH = 0x0160,
            CB_INITSTORAGE = 0x0161,
            CB_MULTIPLEADDSTRING = 0x0163,
            CB_GETCOMBOBOXINFO = 0x0164,
            OCM_COMMAND = 0x2111,
        }

        private enum ComboBoxNotifications
        {
            CBN_ERRSPACE = -1,
            CBN_SELCHANGE = 1,
            CBN_DBLCLK = 2,
            CBN_SETFOCUS = 3,
            CBN_KILLFOCUS = 4,
            CBN_EDITCHANGE = 5,
            CBN_EDITUPDATE = 6,
            CBN_DROPDOWN = 7,
            CBN_CLOSEUP = 8,
            CBN_SELENDOK = 9,
            CBN_SELENDCANCEL = 10,
        }

        private enum ComboBoxStyles
        {
            CBS_SIMPLE = 0x0001,
            CBS_DROPDOWN = 0x0002,
            CBS_DROPDOWNLIST = 0x0003,
            CBS_OWNERDRAWFIXED = 0x0010,
            CBS_OWNERDRAWVARIABLE = 0x0020,
            CBS_AUTOHSCROLL = 0x0040,
            CBS_OEMCONVERT = 0x0080,
            CBS_SORT = 0x0100,
            CBS_HASSTRINGS = 0x0200,
            CBS_NOINTEGRALHEIGHT = 0x0400,
            CBS_DISABLENOSCROLL = 0x0800,
            CBS_UPPERCASE = 0x2000,
            CBS_LOWERCASE = 0x4000,
        }

        public override Font Font
        {
            get => base.Font;

            set
            {
                base.Font = value;
                this.highlightFont.Dispose();
                this.highlightFont = new Font(base.Font, FontStyle.Bold);
            }
        }

        public IList<Person> People
        {
            get
            {
                return this.people;
            }

            set
            {
                var people = value?.ToImmutableList() ?? ImmutableList<Person>.Empty;
                this.UpdateSearchRestoreSelection(Search(this.terms, this.people = people));
            }
        }

        public Person SelectedPerson
        {
            get
            {
                return this.SelectedItem as Person;
            }

            set
            {
                this.SelectedItem = value;
            }
        }

        /// <inheritdoc/>
        protected override CreateParams CreateParams
        {
            get
            {
                var @params = base.CreateParams;
                @params.ExStyle &= ~(int)ComboBoxStyles.CBS_HASSTRINGS;
                return @params;
            }
        }

        protected sealed override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            var item = (Person)this.Items[e.Index];
            var color = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? SystemColors.HighlightText
                : SystemColors.ControlText;

            var termPattern = this.terms.Count == 0
                ? NoMatch
                : new Regex(string.Join("|", this.terms.Union(ToTerms(item.Name).Where(n => this.terms.Any(t => t.IndexOf(n, Comparison) >= 0))).Select(t => Regex.Escape(t))), RegexOptions.IgnoreCase);
            var format = TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
            var bounds = e.Bounds;
            bool DrawString(string text, bool addHighlight = true)
            {
                var index = 0;
                var nextMatch = addHighlight ? termPattern.Match(text) : NoMatch.Match(text);
                while (true)
                {
                    var highlight = false;
                    string chunk;
                    if (!nextMatch.Success)
                    {
                        chunk = text.Substring(index);
                        index = text.Length;
                        nextMatch = null;
                    }
                    else if (nextMatch.Index == index)
                    {
                        highlight = true;
                        chunk = nextMatch.Value;
                        index += nextMatch.Length;
                        nextMatch = termPattern.Match(text, index);
                    }
                    else
                    {
                        chunk = text.Substring(index, nextMatch.Index - index);
                        index = nextMatch.Index;
                    }

                    var font = highlight ? this.highlightFont : e.Font;
                    TextRenderer.DrawText(e.Graphics, chunk, font, bounds, color, format);
                    var size = TextRenderer.MeasureText(e.Graphics, chunk, font, bounds.Size, format);
                    bounds = new Rectangle(bounds.X + size.Width, bounds.Y, bounds.Width - size.Width, bounds.Height);
                    if (bounds.Width <= 0)
                    {
                        return false;
                    }

                    if (index >= text.Length)
                    {
                        return true;
                    }
                }
            }

            if (DrawString(item.Name))
            {
                if (item.Aliases.Count > 0)
                {
                    if (DrawString(" (aka ", addHighlight: false))
                    {
                        var finished = true;
                        var first = true;
                        foreach (var alias in item.Aliases.OrderByDescending(a => termPattern.Matches(a.Name).Cast<Match>().Sum(m => m.Length)))
                        {
                            if (!first)
                            {
                                if (!DrawString(", ", addHighlight: false))
                                {
                                    finished = false;
                                    break;
                                }
                            }

                            if (!DrawString(alias.Name))
                            {
                                finished = false;
                                break;
                            }

                            first = false;
                        }

                        if (finished)
                        {
                            DrawString(")", addHighlight: false);
                        }
                    }
                }
            }

            e.DrawFocusRectangle();
        }

        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            this.OnSelectedIndexChanged(EventArgs.Empty);
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            this.OnSelectedIndexChanged(EventArgs.Empty);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!e.Handled)
            {
                if (!this.DroppedDown)
                {
                    if (!char.IsControl(e.KeyChar))
                    {
                        this.ExpandDropDownRestoreText();
                    }
                }
                else
                {
                    if (e.KeyChar == '\r' || e.KeyChar == '\u001b')
                    {
                        this.CollapseDropDownRestoreSelection();
                        e.Handled = true;
                    }
                }
            }

            base.OnKeyPress(e);
        }

        protected sealed override void OnMeasureItem(MeasureItemEventArgs e)
        {
            var baseFont = this.Font;
            var item = (Person)this.Items[e.Index];

            var termPattern = this.terms.Count == 0
                ? NoMatch
                : new Regex(string.Join("|", this.terms.Union(ToTerms(item.Name).Where(n => this.terms.Any(t => t.IndexOf(n, Comparison) >= 0))).Select(t => Regex.Escape(t))), RegexOptions.IgnoreCase);
            var format = TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;

            var bounds = new Rectangle(Point.Empty, new Size(e.ItemWidth, e.ItemHeight));
            bool MeasureString(string text, bool addHighlight = true)
            {
                var index = 0;
                var nextMatch = addHighlight ? termPattern.Match(text) : NoMatch.Match(text);
                while (true)
                {
                    var highlight = false;
                    string chunk;
                    if (!nextMatch.Success)
                    {
                        chunk = text.Substring(index);
                        index = text.Length;
                        nextMatch = null;
                    }
                    else if (nextMatch.Index == index)
                    {
                        highlight = true;
                        chunk = nextMatch.Value;
                        index += nextMatch.Length;
                        nextMatch = termPattern.Match(text, index);
                    }
                    else
                    {
                        chunk = text.Substring(index, nextMatch.Index - index);
                        index = nextMatch.Index;
                    }

                    var font = highlight ? this.highlightFont : baseFont;
                    var size = TextRenderer.MeasureText(e.Graphics, chunk, font, bounds.Size, format);
                    bounds = new Rectangle(bounds.X + size.Width, bounds.Y, bounds.Width - size.Width, bounds.Height);
                    if (bounds.Width <= 0)
                    {
                        return false;
                    }

                    if (index >= text.Length)
                    {
                        return true;
                    }
                }
            }

            if (MeasureString(item.Name))
            {
                if (item.Aliases.Count > 0)
                {
                    if (MeasureString(" (aka ", addHighlight: false))
                    {
                        var finished = true;
                        var first = true;
                        foreach (var alias in item.Aliases)
                        {
                            if (!first)
                            {
                                if (!MeasureString(", ", addHighlight: false))
                                {
                                    finished = false;
                                    break;
                                }
                            }

                            if (!MeasureString(alias.Name))
                            {
                                finished = false;
                                break;
                            }

                            first = false;
                        }

                        if (finished)
                        {
                            MeasureString(")", addHighlight: false);
                        }
                    }
                }
            }

            e.ItemWidth = bounds.X;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            this.SelectedPersonChanged?.Invoke(this, e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
        }

        protected override void OnTextUpdate(EventArgs e)
        {
            if (this.SelectedItem is Person person && person.Name != this.Text)
            {
                this.ClearSelectedItemRestoreText();
            }

            base.OnTextUpdate(e);

            var value = this.Text;
            this.UpdateSearchRestoreSelection(Search(this.terms = ToTerms(value), this.people));
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)ComboBoxMessages.OCM_COMMAND:
                    switch (((int)(long)m.WParam & 0xFFFF0000) >> 16)
                    {
                        case (int)ComboBoxNotifications.CBN_SELENDOK:
                            this.selEndCancel = false;
                            break;

                        case (int)ComboBoxNotifications.CBN_SELENDCANCEL:
                            this.selEndCancel = true;
                            this.selEndIndex = this.SelectedIndex;
                            this.selEndText = this.Text;
                            this.selEndSelectionStart = this.SelectionStart;
                            this.selEndSelectionLength = this.SelectionLength;
                            break;

                        case (int)ComboBoxNotifications.CBN_CLOSEUP:
                            base.WndProc(ref m);

                            if (this.selEndCancel)
                            {
                                this.SelectedIndex = this.selEndIndex;
                                this.Text = this.selEndText;
                                this.SelectionStart = this.selEndSelectionStart;
                                this.SelectionLength = this.selEndSelectionLength;
                                this.selEndIndex = 0;
                                this.selEndText = string.Empty;
                                this.selEndSelectionStart = 0;
                                this.selEndSelectionLength = 0;
                                this.selEndCancel = false;
                            }

                            return;
                    }

                    break;
            }

            base.WndProc(ref m);
        }

        private static List<Person> Search(HashSet<string> searchTerms, IEnumerable<Person> people)
        {
            return people
                .Select(p =>
                {
                    var nameTerms = ToTerms(p.Name);
                    var allTerms = new HashSet<string>(nameTerms, Comparer);
                    foreach (var alias in p.Aliases)
                    {
                        allTerms.UnionWith(ToTerms(alias.Name));
                    }

                    return new { Person = p, NameTerms = nameTerms, AllTerms = allTerms };
                })
                .OrderByDescending(p => p.NameTerms.SetEquals(searchTerms))
                .ThenByDescending(p => p.NameTerms.IsSupersetOf(searchTerms))
                .ThenByDescending(p => p.AllTerms.IsSupersetOf(searchTerms))
                .ThenByDescending(p => Math.Max(
                    p.AllTerms.Count(n => searchTerms.Contains(n)),
                    searchTerms.Count(n => p.AllTerms.Contains(n))))
                .ThenByDescending(p => Math.Max(
                    p.AllTerms.Count(n => searchTerms.Any(t => t.IndexOf(n, Comparison) >= 0)),
                    searchTerms.Count(n => p.AllTerms.Any(t => t.IndexOf(n, Comparison) >= 0))))
                .ThenBy(p => p.Person.Name, Comparer)
                .Select(p => p.Person)
                .ToList();
        }

        private static HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), Comparer);

        private void ClearSelectedItemRestoreText()
        {
            var text = this.Text;
            var selectionStart = this.SelectionStart;
            var selectionLength = this.SelectionLength;

            this.BeginUpdate();
            this.SelectedIndex = -1;
            this.Text = text;
            this.SelectionStart = selectionStart;
            this.SelectionLength = selectionLength;
            this.EndUpdate();
        }

        private void CollapseDropDownRestoreSelection()
        {
            var index = this.SelectedIndex;
            var text = this.Text;
            var selectionStart = this.SelectionStart;
            var selectionLength = this.SelectionLength;

            this.BeginUpdate();
            this.DroppedDown = false;

            if (index == -1 && this.SelectedIndex != index)
            {
                this.SelectedIndex = index;
                this.Text = text;
                this.SelectionStart = selectionStart;
                this.SelectionLength = selectionLength;
            }

            this.EndUpdate();
        }

        private void ExpandDropDownRestoreText()
        {
            var text = this.Text;
            var selectionStart = this.SelectionStart;
            var selectionLength = this.SelectionLength;

            this.BeginUpdate();
            this.DroppedDown = true;

            this.Text = text;
            this.SelectionStart = selectionStart;
            this.SelectionLength = selectionLength;
            this.EndUpdate();
        }

        private void UpdateSearchRestoreSelection(IEnumerable<Person> people)
        {
            var selectedItem = this.SelectedPerson;
            var text = this.Text;
            var selectionStart = this.SelectionStart;
            var selectionLength = this.SelectionLength;

            var peopleArray = people.ToArray();
            this.BeginUpdate();
            this.Items.Clear();
            this.Items.AddRange(peopleArray);

            this.SelectedIndex = Array.IndexOf(peopleArray, selectedItem);
            this.Text = text;
            this.SelectionStart = selectionStart;
            this.SelectionLength = selectionLength;
            this.EndUpdate();
        }
    }
}
