// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TaggingLibrary;

    public partial class TagSearchBox : SearchBoxBase<TagInfo>
    {
        private static readonly StringComparer Comparer = StringComparer.CurrentCultureIgnoreCase;
        private static readonly StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;

        protected override IList<(string part, bool highlight)> RenderItem(HashSet<string> terms, TagInfo item)
        {
            var pattern = terms.Count == 0 ? NoMatch : new Regex(string.Join("|", terms.Select(Regex.Escape)), RegexOptions.IgnoreCase);
            var list = new List<(string part, bool highlight)>();

            list.AddRange(HighlightString(pattern, item.Tag));
            if (item.Aliases.Count > 0)
            {
                list.Add((" (", false));

                var first = true;
                foreach (var alias in item.Aliases.OrderByDescending(a => pattern.Matches(a).Cast<Match>().Sum(m => m.Length)))
                {
                    if (!first)
                    {
                        list.Add((", ", false));
                    }

                    list.AddRange(HighlightString(pattern, alias));
                    first = false;
                }

                list.Add((")", false));
            }

            return list;
        }

        protected override List<TagInfo> Search(HashSet<string> searchTerms, IEnumerable<TagInfo> tags)
        {
            return tags
                .Select(t =>
                {
                    var tagTerms = this.ToTerms(t.Tag);
                    var allTerms = new HashSet<string>(tagTerms, Comparer);
                    foreach (var alias in t.Aliases)
                    {
                        allTerms.UnionWith(this.ToTerms(alias));
                    }

                    return new { TagInfo = t, TagTerms = tagTerms, AllTerms = allTerms };
                })
                .OrderByDescending(t => t.TagTerms.SetEquals(searchTerms))
                .ThenByDescending(t => t.TagTerms.IsSupersetOf(searchTerms))
                .ThenByDescending(t => t.AllTerms.IsSupersetOf(searchTerms))
                .ThenByDescending(t => Math.Max(
                    t.AllTerms.Count(n => searchTerms.Contains(n)),
                    searchTerms.Count(n => t.AllTerms.Contains(n))))
                .ThenByDescending(t => Math.Max(
                    t.AllTerms.Count(n => searchTerms.Any(s => s.IndexOf(n, Comparison) >= 0)),
                    searchTerms.Count(n => t.AllTerms.Any(s => s.IndexOf(n, Comparison) >= 0))))
                .ThenBy(t => t.TagInfo.Tag, Comparer)
                .Select(t => t.TagInfo)
                .ToList();
        }

        protected override HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), Comparer);
    }
}
