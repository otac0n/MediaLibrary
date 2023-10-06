// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
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

        protected override IList<(string part, Highlighting highlight)> RenderItem(HashSet<string> terms, Person item)
        {
            var pattern = terms.Count == 0 ? NoMatch : new Regex(string.Join("|", terms.Union(this.ToTerms(item.Name).Where(n => terms.Any(t => t.IndexOf(n, Comparison) >= 0))).Select(Regex.Escape)), RegexOptions.IgnoreCase);
            var list = new List<(string part, Highlighting highlight)>();

            list.AddRange(HighlightString(pattern, item.Name));
            if (item.Aliases.Count > 0)
            {
                list.Add((" (aka ", Highlighting.None));

                var first = true;
                foreach (var alias in item.Aliases.OrderByDescending(a => pattern.Matches(a.Name).Cast<Match>().Sum(m => m.Length)))
                {
                    if (!first)
                    {
                        list.Add((", ", Highlighting.None));
                    }

                    list.AddRange(HighlightString(pattern, alias.Name));
                    first = false;
                }

                list.Add((")", Highlighting.None));
            }

            return list;
        }

        protected override IEnumerable<Person> Search(HashSet<string> searchTerms, IEnumerable<Person> people)
        {
            return people
                .Select(p =>
                {
                    var nameTerms = this.ToTerms(p.Name);
                    return new
                    {
                        Person = p,
                        NameTerms = nameTerms,
                        NameScore = Score(searchTerms, nameTerms, Comparison),
                        Aliases = p.Aliases.ToDictionary(a => a, a =>
                        {
                            var aliasTerms = this.ToTerms(a.Name);
                            return new
                            {
                                AliasTerms = aliasTerms,
                                AliasScore = Score(searchTerms, aliasTerms, Comparison),
                            };
                        }),
                    };
                })
                .OrderByDescending(p => p.NameTerms.SetEquals(searchTerms))
                .ThenByDescending(p => p.Aliases.Values.Any(a => a.AliasTerms.SetEquals(searchTerms)))
                .ThenByDescending(p => new[] { p.NameScore }.Concat(p.Aliases.Values.Select(a => a.AliasScore)).Max())
                .ThenByDescending(p => p.NameScore)
                .ThenBy(p => p.Person.Name, Comparer)
                .Select(p => p.Person);
        }

        protected override HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), Comparer);
    }
}
