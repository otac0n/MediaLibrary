// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MediaLibrary.Storage;

    public partial class PersonSearchBox : SearchBoxBase<Person>
    {
        private static readonly StringComparer Comparer = StringComparer.CurrentCultureIgnoreCase;
        private static readonly StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;

        protected override IList<(string part, bool highlight)> RenderItem(HashSet<string> terms, Person item)
        {
            var pattern = terms.Count == 0 ? NoMatch : new Regex(string.Join("|", terms.Union(this.ToTerms(item.Name).Where(n => terms.Any(t => t.IndexOf(n, Comparison) >= 0))).Select(Regex.Escape)), RegexOptions.IgnoreCase);
            var list = new List<(string part, bool highlight)>();

            list.AddRange(HighlightString(pattern, item.Name));
            if (item.Aliases.Count > 0)
            {
                list.Add((" (aka ", false));

                var first = true;
                foreach (var alias in item.Aliases.OrderByDescending(a => pattern.Matches(a.Name).Cast<Match>().Sum(m => m.Length)))
                {
                    if (!first)
                    {
                        list.Add((", ", false));
                    }

                    list.AddRange(HighlightString(pattern, alias.Name));
                    first = false;
                }

                list.Add((")", false));
            }

            return list;
        }

        protected override List<Person> Search(HashSet<string> searchTerms, IEnumerable<Person> people)
        {
            return people
                .Select(p =>
                {
                    var nameTerms = this.ToTerms(p.Name);
                    var allTerms = new HashSet<string>(nameTerms, Comparer);
                    foreach (var alias in p.Aliases)
                    {
                        allTerms.UnionWith(this.ToTerms(alias.Name));
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

        protected override HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), Comparer);
    }
}
