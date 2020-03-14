// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    public sealed class TagRuleEngine
    {
        private readonly List<TagRule> tagRules = new List<TagRule>();

        public TagRuleEngine(IEnumerable<TagRule> rules)
        {
            foreach (var rule in rules)
            {
                if ((rule.Operator == TagOperator.Definition || rule.Operator == TagOperator.Specialization) &&
                    (rule.Left.Count > 1 || rule.Right.Count > 1))
                {
                    throw new ArgumentOutOfRangeException(nameof(rules));
                }
                else
                {
                    this.tagRules.Add(rule);
                }
            }
        }

        public AnalysisResult Analyze(IEnumerable<string> tags)
        {
            var tagsSet = new HashSet<string>(tags.Select(t => t.TrimStart('#')));
            if (tagsSet.Count == 0)
            {
                return AnalysisResult.Empty;
            }

            var isDefinition = this.tagRules.ToLookup(r => r.Operator == TagOperator.Definition);
            var renameMap = new Dictionary<string, string>();
            var reverseMap = new Dictionary<string, List<string>>();
            foreach (var rule in isDefinition[true])
            {
                var fromTag = rule.Left.Single();
                var toTag = rule.Right.Single();
                while (renameMap.TryGetValue(toTag, out var nextTag))
                {
                    toTag = nextTag;
                }

                renameMap[fromTag] = toTag;

                if (!reverseMap.TryGetValue(toTag, out var destinationReverse))
                {
                    reverseMap[toTag] = destinationReverse = new List<string>();
                }

                if (reverseMap.TryGetValue(fromTag, out var connected))
                {
                    foreach (var c in connected)
                    {
                        renameMap[c] = toTag;
                    }

                    destinationReverse.AddRange(connected);
                    reverseMap.Remove(fromTag);
                }

                destinationReverse.Add(fromTag);
            }

            var transformedRules = SimplifyRules(isDefinition[false], renameMap).ToLookup(r => r.Operator);

            var normalizedTags = ImmutableHashSet.CreateRange(tagsSet.Select(tag => renameMap.TryGetValue(tag, out var renamed) ? renamed : tag));
            var effectiveTags = normalizedTags;
            var changed = true;
            while (changed)
            {
                changed = false;
                var groups = from rule in transformedRules[TagOperator.Specialization]
                             where effectiveTags.IsSupersetOf(rule.Left)
                             select rule;
                foreach (var rule in groups)
                {
                    var toAdd = rule.Right.Single();
                    if (!effectiveTags.Contains(toAdd))
                    {
                        effectiveTags = effectiveTags.Add(toAdd);
                        changed = true;
                    }
                }
            }

            var missingTagSets = ImmutableList<ImmutableHashSet<string>>.Empty;
            var effectiveAndSingleMissingTags = new HashSet<string>(effectiveTags);
            changed = true;
            while (changed)
            {
                changed = false;
                var groups = from rule in transformedRules[TagOperator.Implication]
                             where effectiveAndSingleMissingTags.IsSupersetOf(rule.Left)
                             where !rule.Right.Overlaps(effectiveAndSingleMissingTags)
                             group rule by rule.Right.Count == 1 into g
                             orderby g.Key descending
                             select g;
                var firstGroup = groups.FirstOrDefault();
                if (firstGroup != null)
                {
                    foreach (var rule in firstGroup)
                    {
                        missingTagSets = missingTagSets.Add(rule.Right);
                        if (rule.Right.Count == 1)
                        {
                            if (changed |= effectiveAndSingleMissingTags.Add(rule.Right.Single()))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            var suggestedTags = ImmutableHashSet.CreateRange(missingTagSets.SelectMany(s => s));
            foreach (var rule in transformedRules[TagOperator.Suggestion])
            {
                if (effectiveAndSingleMissingTags.IsSupersetOf(rule.Left) && !effectiveAndSingleMissingTags.Overlaps(rule.Right))
                {
                    suggestedTags = suggestedTags.Union(rule.Right);
                }
            }

            return new AnalysisResult(
                normalizedTags,
                effectiveTags,
                missingTagSets,
                suggestedTags);
        }

        private static IEnumerable<TagRule> SimplifyRules(IEnumerable<TagRule> rules, Dictionary<string, string> renameMap)
        {
            string R(string tag) => renameMap.TryGetValue(tag, out var renamed) ? renamed : tag;

            foreach (var r in rules)
            {
                TagRule rule;
                if (r.Left.Any(renameMap.ContainsKey) || r.Right.Any(renameMap.ContainsKey))
                {
                    rule = new TagRule(
                        ImmutableHashSet.CreateRange(r.Left.Select(R)),
                        r.Operator,
                        ImmutableHashSet.CreateRange(r.Right.Select(R)));
                }
                else
                {
                    rule = r;
                }

                if (rule.Operator == TagOperator.BidirectionalImplication ||
                    rule.Operator == TagOperator.BidirectionalSuggestion)
                {
                    var singleDirection = (TagOperator)((int)r.Operator - 1);
                    yield return new TagRule(r.Left, singleDirection, r.Right);
                    foreach (var newLeft in rule.Right)
                    {
                        foreach (var newRight in rule.Left)
                        {
                            yield return new TagRule(newLeft, singleDirection, newRight);
                        }
                    }
                }
                else
                {
                    yield return rule;
                }
            }
        }

        public class AnalysisResult
        {
            public static AnalysisResult Empty = new AnalysisResult(
                ImmutableHashSet<string>.Empty,
                ImmutableHashSet<string>.Empty,
                ImmutableList<ImmutableHashSet<string>>.Empty,
                ImmutableHashSet<string>.Empty);

            public AnalysisResult(ImmutableHashSet<string> normalizedTags, ImmutableHashSet<string> effectiveTags, ImmutableList<ImmutableHashSet<string>> missingTagSets, ImmutableHashSet<string> suggestedTags)
            {
                this.NormalizedTags = normalizedTags;
                this.EffectiveTags = effectiveTags;
                this.MissingTagSets = missingTagSets;
                this.SuggestedTags = suggestedTags;
            }

            public ImmutableHashSet<string> EffectiveTags { get; }

            public ImmutableList<ImmutableHashSet<string>> MissingTagSets { get; }

            public ImmutableHashSet<string> NormalizedTags { get; }

            public ImmutableHashSet<string> SuggestedTags { get; }
        }
    }
}
