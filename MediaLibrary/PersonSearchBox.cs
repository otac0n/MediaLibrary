// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class PersonSearchBox : UserControl
    {
        private static StringComparer Comparer = StringComparer.CurrentCultureIgnoreCase;
        private static StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;
        private static Regex NoMatch = new Regex("(?!)");

        private ImmutableList<Person> people = ImmutableList<Person>.Empty;
        private HashSet<string> terms = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public PersonSearchBox()
        {
            this.InitializeComponent();
        }

        public event EventHandler<EventArgs> SearchTextChanged;

        public event EventHandler<EventArgs> SelectedPersonChanged;

        public IList<Person> People
        {
            get
            {
                return this.people;
            }

            set
            {
                var people = value?.ToImmutableList() ?? ImmutableList<Person>.Empty;
                if (!people.Contains(this.SelectedPerson))
                {
                    this.SelectedPerson = null;
                }

                this.UpdateSearch(Search(this.terms, this.people = people));
            }
        }

        public Person SelectedPerson
        {
            get
            {
                return this.searchBox.SelectedItem as Person;
            }

            set
            {
                this.searchBox.SelectedItem = value;
            }
        }

        public override string Text
        {
            get { return this.searchBox.Text; }
            set { this.searchBox.Text = value; }
        }

        private static List<Person> Search(HashSet<string> searchTerms, IEnumerable<Person> people)
        {
            return people
                .Select(p => new { Person = p, NameTerms = ToTerms(p.Name) })
                .OrderByDescending(p => p.NameTerms.SetEquals(searchTerms))
                .ThenByDescending(p => p.NameTerms.IsSupersetOf(searchTerms))
                .ThenByDescending(p => Math.Max(
                    p.NameTerms.Count(n => searchTerms.Contains(n)),
                    searchTerms.Count(n => p.NameTerms.Contains(n))))
                .ThenByDescending(p => Math.Max(
                    p.NameTerms.Count(n => searchTerms.Any(t => t.IndexOf(n, Comparison) >= 0)),
                    searchTerms.Count(n => p.NameTerms.Any(t => t.IndexOf(n, Comparison) >= 0))))
                .ThenBy(p => p.Person.Name, Comparer)
                .Select(p => p.Person)
                .ToList();
        }

        private static HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), StringComparer.CurrentCultureIgnoreCase);

        private void SearchBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            var item = (Person)this.searchBox.Items[e.Index];
            var highlightFont = new Font(e.Font, FontStyle.Bold);
            var color = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? SystemColors.HighlightText
                : SystemColors.ControlText;

            var termPattern = this.terms.Count == 0
                ? NoMatch
                : new Regex(string.Join("|", this.terms.Union(ToTerms(item.Name).Where(n => this.terms.Any(t => t.IndexOf(n, Comparison) >= 0))).Select(t => Regex.Escape(t))), RegexOptions.IgnoreCase);
            var format = TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
            var bounds = e.Bounds;
            void DrawString(string text)
            {
                var index = 0;
                var nextMatch = termPattern.Match(text);
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

                    var font = highlight ? highlightFont : e.Font;
                    TextRenderer.DrawText(e.Graphics, chunk, font, bounds, color, format);
                    var size = TextRenderer.MeasureText(e.Graphics, chunk, font, bounds.Size, format);
                    bounds = new Rectangle(bounds.X + size.Width, bounds.Y, bounds.Width - size.Width, bounds.Height);
                    if (index >= text.Length || bounds.Left <= 0)
                    {
                        break;
                    }
                }
            }

            DrawString(item.Name);

            e.DrawFocusRectangle();
        }

        private void SearchBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
        }

        private void SearchBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            this.SelectedPersonChanged?.Invoke(this, e);
        }

        private void SearchBox_TextUpdate(object sender, EventArgs e)
        {
            this.terms = ToTerms(this.searchBox.Text);
            this.UpdateSearch(Search(this.terms, this.people));
        }

        private void UpdateSearch(IEnumerable<Person> people)
        {
            var selectionStart = this.searchBox.SelectionStart;
            var selectionLength = this.searchBox.SelectionLength;
            this.searchBox.Items.Clear();
            this.searchBox.Items.AddRange(people.ToArray());
            this.searchBox.SelectionStart = selectionStart;
            this.searchBox.SelectionLength = selectionLength;
        }
    }
}
