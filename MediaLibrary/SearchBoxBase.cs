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

    public abstract class SearchBoxBase<TItem> : ComboBox
        where TItem : class
    {
        protected static readonly Regex NoMatch = new Regex("(?!)");
        private Font highlightFont;

        private ImmutableList<TItem> items = ImmutableList<TItem>.Empty;

        private bool selEndCancel;
        private int selEndIndex;
        private int selEndSelectionLength;
        private int selEndSelectionStart;
        private string selEndText;

        private HashSet<string> terms = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public SearchBoxBase()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.highlightFont = new Font(this.Font, FontStyle.Bold);
        }

        public new event EventHandler<EventArgs> SelectedItemChanged;

        public enum Highlighting
        {
            None,
            Highlighted,
            Subdued,
        }

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

        public new IList<TItem> Items
        {
            get
            {
                return this.items;
            }

            set
            {
                var people = value?.ToImmutableList() ?? ImmutableList<TItem>.Empty;
                this.UpdateSearchRestoreSelection(this.Search(this.terms, this.items = people));
            }
        }

        public new TItem SelectedItem
        {
            get
            {
                return base.SelectedItem as TItem;
            }

            set
            {
                base.SelectedItem = value;
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

        protected static IEnumerable<(string part, Highlighting highlight)> HighlightString(Regex pattern, string value)
        {
            var index = 0;
            var nextMatch = pattern.Match(value);
            while (index < value.Length)
            {
                var highlight = false;
                string chunk;
                if (!nextMatch.Success)
                {
                    chunk = value.Substring(index);
                    index = value.Length;
                    nextMatch = null;
                }
                else if (nextMatch.Index == index)
                {
                    highlight = true;
                    chunk = nextMatch.Value;
                    index += nextMatch.Length;
                    nextMatch = pattern.Match(value, index);
                }
                else
                {
                    chunk = value.Substring(index, nextMatch.Index - index);
                    index = nextMatch.Index;
                }

                yield return (chunk, highlight ? Highlighting.Highlighted : Highlighting.None);
            }
        }

        protected static (int match, int starts, int contains) Score(HashSet<string> search, HashSet<string> subject, StringComparison comparison)
        {
            var match = 0;
            var starts = 0;
            var contains = 0;
            foreach (var term in search)
            {
                if (subject.Contains(term))
                {
                    match++;
                }
                else
                {
                    var index = (from s in subject
                                 let ix = s.IndexOf(term, comparison)
                                 where ix >= 0
                                 select (int?)ix).Min();
                    if (index == 0)
                    {
                        starts++;
                    }
                    else
                    {
                        contains++;
                    }
                }
            }

            return (match, starts, contains);
        }

        protected override sealed void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            var item = (TItem)base.Items[e.Index];
            var rendered = this.RenderItem(this.terms, item);
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var baseColor = selected
                ? SystemColors.HighlightText
                : SystemColors.ControlText;

            var format = TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
            var bounds = e.Bounds;
            foreach (var (text, highlight) in rendered)
            {
                var font = highlight == Highlighting.Highlighted ? this.highlightFont : e.Font;
                var color = !selected && highlight == Highlighting.Subdued ? SystemColors.GrayText : baseColor;
                TextRenderer.DrawText(e.Graphics, text.Replace("&", "&&"), font, bounds, color, format);
                var size = TextRenderer.MeasureText(e.Graphics, text, font, bounds.Size, format);
                bounds = new Rectangle(bounds.X + size.Width, bounds.Y, bounds.Width - size.Width, bounds.Height);
                if (bounds.Width <= 0)
                {
                    break;
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

        protected override sealed void OnMeasureItem(MeasureItemEventArgs e)
        {
            var baseFont = this.Font;
            var item = (TItem)base.Items[e.Index];
            var rendered = this.RenderItem(this.terms, item);

            var format = TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
            var bounds = new Rectangle(Point.Empty, new Size(e.ItemWidth, e.ItemHeight));
            foreach (var (text, highlight) in rendered)
            {
                var font = highlight == Highlighting.Highlighted ? this.highlightFont : baseFont;
                var size = TextRenderer.MeasureText(e.Graphics, text.Replace("&", "&&"), font, bounds.Size, format);
                bounds = new Rectangle(bounds.X + size.Width, bounds.Y, bounds.Width - size.Width, bounds.Height);
                if (bounds.Width <= 0)
                {
                    break;
                }
            }

            e.ItemWidth = bounds.X;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            this.SelectedItemChanged?.Invoke(this, e);
        }

        protected override void OnTextUpdate(EventArgs e)
        {
            var value = this.Text;

            if (base.SelectedItem is TItem item && item.ToString() != value)
            {
                this.ClearSelectedItemRestoreText();
            }

            base.OnTextUpdate(e);

            this.UpdateSearchRestoreSelection(this.Search(this.terms = this.ToTerms(value), this.items));
        }

        protected virtual IList<(string part, Highlighting highlight)> RenderItem(HashSet<string> terms, TItem item)
        {
            var pattern = terms.Count == 0 ? NoMatch : new Regex(string.Join("|", terms.Select(Regex.Escape)));
            return HighlightString(pattern, item.ToString()).ToList();
        }

        protected abstract IEnumerable<TItem> Search(HashSet<string> searchTerms, IEnumerable<TItem> people);

        protected abstract HashSet<string> ToTerms(string name);

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

        private void UpdateSearchRestoreSelection(IEnumerable<TItem> items)
        {
            if (!this.IsDisposed)
            {
                var selectedItem = this.SelectedItem;
                var text = this.Text;
                var selectionStart = this.SelectionStart;
                var selectionLength = this.SelectionLength;

                var itemArray = items
                    .OrderByDescending(i => i.ToString().StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                    .ToArray();
                this.BeginUpdate();
                base.Items.Clear();
                base.Items.AddRange(itemArray);

                this.SelectedIndex = Array.IndexOf(itemArray, selectedItem);
                this.Text = text;
                this.SelectionStart = selectionStart;
                this.SelectionLength = selectionLength;
                this.EndUpdate();
            }
        }
    }
}
