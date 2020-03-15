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
        private readonly Dictionary<string, string> renameMap = new Dictionary<string, string>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationChildMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly Dictionary<string, ImmutableHashSet<string>> specializationParentMap = new Dictionary<string, ImmutableHashSet<string>>();
        private readonly ILookup<TagOperator, TagRule> tagRules;

        public TagRuleEngine(IEnumerable<TagRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var nonDefinitionRules = new List<TagRule>();
            var reverseRenameMap = new Dictionary<string, List<string>>();
            foreach (var rule in rules)
            {
                if (rule.Operator == TagOperator.Definition || rule.Operator == TagOperator.Specialization)
                {
                    if (rule.Left.Count > 1 || rule.Right.Count > 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(rules), $"The operator '{rule.Operator}' requires a single tag on both the left and right hand sides in rule '{rule}'");
                    }

                    if (rule.Operator == TagOperator.Definition)
                    {
                        var fromTag = rule.Left.Single();
                        var toTag = rule.Right.Single();
                        while (this.renameMap.TryGetValue(toTag, out var nextTag))
                        {
                            toTag = nextTag;
                        }

                        this.renameMap[fromTag] = toTag;

                        if (!reverseRenameMap.TryGetValue(toTag, out var destinationReverse))
                        {
                            reverseRenameMap[toTag] = destinationReverse = new List<string>();
                        }

                        if (reverseRenameMap.TryGetValue(fromTag, out var connected))
                        {
                            foreach (var c in connected)
                            {
                                this.renameMap[c] = toTag;
                            }

                            destinationReverse.AddRange(connected);
                            reverseRenameMap.Remove(fromTag);
                        }

                        destinationReverse.Add(fromTag);
                        continue;
                    }
                }

                nonDefinitionRules.Add(rule);
            }

            this.tagRules = this.SimplifyRules(nonDefinitionRules).ToLookup(r => r.Operator);

            foreach (var rule in this.tagRules[TagOperator.Specialization])
            {
                var fromTag = rule.Left.Single();
                var toTag = rule.Right.Single();

                AddParentToChildren(fromTag, toTag, this.specializationChildMap, this.specializationParentMap);
                AddParentToChildren(toTag, fromTag, this.specializationParentMap, this.specializationChildMap);
            }
        }

        public AnalysisResult Analyze(IEnumerable<string> tags)
        {
            var normalizedTags = ImmutableHashSet.CreateRange(tags.Select(tag => this.Rename(tag.TrimStart('#'))));
            if (normalizedTags.Count == 0)
            {
                return AnalysisResult.Empty;
            }

            var ruleLookup = this.tagRules;

            var effectiveTags = normalizedTags;
            foreach (var tag in effectiveTags)
            {
                if (this.specializationParentMap.TryGetValue(tag, out var specializes))
                {
                    effectiveTags = effectiveTags.Union(specializes);
                }
            }

            var missingTagSets = ImmutableList<ImmutableHashSet<string>>.Empty;
            var effectiveAndSingleMissingTags = new HashSet<string>(effectiveTags);
            var changed = true;
            while (changed)
            {
                changed = false;
                var groups = from rule in ruleLookup[TagOperator.Implication]
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
                            var right = rule.Right.Single();
                            if (effectiveAndSingleMissingTags.Add(right))
                            {
                                if (this.specializationParentMap.TryGetValue(right, out var specializes))
                                {
                                    effectiveAndSingleMissingTags.UnionWith(specializes);
                                }

                                changed = true;
                                break;
                            }
                        }
                    }
                }
            }

            var suggestedTags = ImmutableHashSet.CreateRange(missingTagSets.SelectMany(s => s));
            foreach (var rule in ruleLookup[TagOperator.Suggestion])
            {
                if (effectiveAndSingleMissingTags.IsSupersetOf(rule.Left) && !effectiveAndSingleMissingTags.Overlaps(rule.Right))
                {
                    suggestedTags = suggestedTags.Union(rule.Right);
                }
            }

            foreach (var tag in effectiveAndSingleMissingTags)
            {
                if (this.specializationChildMap.TryGetValue(tag, out var children) &&
                    !effectiveAndSingleMissingTags.Overlaps(children))
                {
                    suggestedTags = suggestedTags.Union(children);
                }
            }

            return new AnalysisResult(
                normalizedTags,
                effectiveTags,
                missingTagSets,
                suggestedTags);
        }

        private static void AddParentToChildren(string parent, string child, Dictionary<string, ImmutableHashSet<string>> parentMap, Dictionary<string, ImmutableHashSet<string>> childMap)
        {
            if (!parentMap.TryGetValue(child, out var parents))
            {
                parents = ImmutableHashSet<string>.Empty;
            }

            if (parentMap.TryGetValue(parent, out var grandparents))
            {
                parents = parents.Union(grandparents);
            }

            parents = parents.Add(parent);

            var queue = new Queue<string>();
            var seen = new HashSet<string>();
            queue.Enqueue(child);
            while (queue.Count > 0)
            {
                var currentChild = queue.Dequeue();
                if (!seen.Add(currentChild))
                {
                    continue;
                }

                if (!parentMap.TryGetValue(currentChild, out var currentChildParents))
                {
                    currentChildParents = ImmutableHashSet<string>.Empty;
                }

                parentMap[currentChild] = currentChildParents.Union(parents.Remove(currentChild));

                if (childMap.TryGetValue(currentChild, out var grandchildren))
                {
                    foreach (var grandchild in grandchildren)
                    {
                        queue.Enqueue(grandchild);
                    }
                }
            }
        }

        private string Rename(string tag) =>
            this.renameMap.TryGetValue(tag, out var renamed) ? renamed : tag;

        private IEnumerable<TagRule> SimplifyRules(IEnumerable<TagRule> rules)
        {
            foreach (var r in rules)
            {
                TagRule rule;
                if (r.Left.Any(this.renameMap.ContainsKey) || r.Right.Any(this.renameMap.ContainsKey))
                {
                    rule = new TagRule(
                        ImmutableHashSet.CreateRange(r.Left.Select(this.Rename)),
                        r.Operator,
                        ImmutableHashSet.CreateRange(r.Right.Select(this.Rename)));
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
