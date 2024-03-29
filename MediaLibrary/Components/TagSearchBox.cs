// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TaggingLibrary;

    public partial class TagSearchBox : SearchBoxBase<TagInfo>
    {
        private static readonly StringComparer Comparer = StringComparer.CurrentCultureIgnoreCase;
        private static readonly StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;

        public TagRuleEngine Engine { get; set; }

        protected override IList<(string part, Highlighting highlight)> RenderItem(HashSet<string> terms, TagInfo item)
        {
            var pattern = terms.Count == 0 ? NoMatch : new Regex(string.Join("|", terms.Select(Regex.Escape)), RegexOptions.IgnoreCase);
            var list = new List<(string part, Highlighting highlight)>();

            list.AddRange(HighlightString(pattern, item.Tag));
            if (item.Aliases.Count > 0)
            {
                list.Add((" (", Highlighting.None));

                var first = true;
                foreach (var alias in item.Aliases.OrderByDescending(a => pattern.Matches(a).Cast<Match>().Sum(m => m.Length)))
                {
                    if (!first)
                    {
                        list.Add((", ", Highlighting.None));
                    }

                    list.AddRange(HighlightString(pattern, alias));
                    first = false;
                }

                list.Add((")", Highlighting.None));
            }

            var properties = (this.Engine?.GetAllTagProperties(item.Tag) ?? item.Properties)
                .Where(p =>
                    p != TagRuleEngine.AbstractProperty &&
                    !p.StartsWith("color=", StringComparison.Ordinal) &&
                    !p.StartsWith("order=", StringComparison.Ordinal))
                .Distinct()
                .ToList();
            if (properties.Count > 0)
            {
                list.Add(($" [{string.Join(", ", properties)}]", Highlighting.Subdued));
            }

            if (item.Ancestors.Count > 0)
            {
                list.Add(($" :: {string.Join(", ", item.Ancestors)}", Highlighting.Subdued));
            }

            return list;
        }

        protected override IEnumerable<TagInfo> Search(HashSet<string> searchTerms, IEnumerable<TagInfo> tags)
        {
            return tags
                .Select(t =>
                {
                    var tagTerms = this.ToTerms(t.Tag);
                    return new
                    {
                        TagInfo = t,
                        TagTerms = tagTerms,
                        TagScore = Score(searchTerms, tagTerms, Comparison),
                        Aliases = t.Aliases.ToDictionary(a => a, a =>
                        {
                            var aliasTerms = this.ToTerms(a);
                            return new
                            {
                                AliasTerms = aliasTerms,
                                AliasScore = Score(searchTerms, aliasTerms, Comparison),
                            };
                        }),
                    };
                })
                .OrderByDescending(t => t.Aliases.Values.Any(a => a.AliasTerms.SetEquals(searchTerms)))
                .ThenByDescending(t => new[] { t.TagScore }.Concat(t.Aliases.Values.Select(a => a.AliasScore)).Max())
                .ThenByDescending(t => t.TagScore)
                .ThenBy(t => t.TagInfo.Tag, Comparer)
                .Select(t => t.TagInfo);
        }

        protected override HashSet<string> ToTerms(string name) =>
            new HashSet<string>(Regex.Matches(name, @"\w+").Cast<Match>().Select(m => m.Value), Comparer);
    }
}
