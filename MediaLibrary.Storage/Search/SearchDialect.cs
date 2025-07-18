﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MediaLibrary.Search;
    using MediaLibrary.Search.Terms;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search.Expressions;
    using TaggingLibrary;

    public sealed class SearchDialect : QueryCompiler<Expression>
    {
        public static readonly string HiddenTag = "hidden";

        private static readonly Term ExcludeHiddenTerm = new NegationTerm(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, HiddenTag));

        private ImmutableList<(double? min, double? max)> starRanges;

        public SearchDialect(TagRuleEngine tagEngine, ImmutableList<(double? min, double? max)> starRanges, Func<string, Term> getSavedSearch)
            : base(getSavedSearch)
        {
            this.TagEngine = tagEngine;
            this.starRanges = starRanges;
        }

        private TagRuleEngine TagEngine { get; }

        public Expression CompileQuery(Term term, bool excludeHidden = true)
        {
            var expression = this.Compile(term);

            if (excludeHidden)
            {
                // An empty search would usually return everything, but this is bad for performance and causes user anxiety.
                if (expression is ConjunctionExpression conjunction && conjunction.Expressions.Count == 0)
                {
                    expression = new DisjunctionExpression(conjunction.Expressions);
                }
                else
                {
                    // Hidden tags should be excluded unless explicitly requested.
                    var containsHidden = new ContainsHiddenReplacer(this.TagEngine).Replace(expression);
                    if (!containsHidden)
                    {
                        expression = new ConjunctionExpression(ImmutableList.Create(
                            expression,
                            this.Compile(ExcludeHiddenTerm)));
                    }
                }
            }

            return expression;
        }

        /// <inheritdoc/>
        public override Expression CompileConjunction(IEnumerable<Expression> query) =>
            new ConjunctionExpression(query.ToImmutableList());

        /// <inheritdoc/>
        public override Expression CompileDisjunction(IEnumerable<Expression> query) =>
            new DisjunctionExpression(query.ToImmutableList());

        /// <inheritdoc/>
        public override Expression CompileField(FieldTerm field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            static Regex GlobToRegex(string tag) =>
                new Regex(
                    string.Join(".*", tag.Split('*').Select(Regex.Escape)), // TODO: Remove catastrophic backtracking from "**"
                    RegexOptions.Singleline);

            // Tag globbing.
            if (field.Value.Contains('*', StringComparison.Ordinal))
            {
                switch (field.Field)
                {
                    case "tag":
                    case "rejected":
                        // TODO: Handle the above in the DB engine for sargability?
                    case "~":
                    case "suggested":
                    case "missing":
                    case "add":
                    case "*":
                        var regex = GlobToRegex(field.Value);
                        var matching = this.TagEngine.GetKnownTags().Where((Func<string, bool>)regex.IsMatch);
                        return this.CompileDisjunction(
                            matching.Select(t => this.CompileField(new FieldTerm(field.Field, t))));
                }
            }

            switch (field.Field)
            {
                case null:
                    return new TextExpression(field.Value);

                case "@":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    if (int.TryParse(field.Value, out var personId))
                    {
                        if (personId == 0)
                        {
                            return NoPeopleExpression.Instance;
                        }
                        else
                        {
                            return new PersonIdExpression(personId);
                        }
                    }
                    else
                    {
                        return new PersonNameExpression(field.Value);
                    }

                case "type":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    var ix = field.Value.IndexOf('/');
                    if (ix < 0)
                    {
                        return this.CompileDisjunction(
                            new TypeEqualsExpression(field.Value),
                            new TypePrefixExpression(field.Value + "/"));
                    }
                    else if (ix == field.Value.Length - 1)
                    {
                        return new TypePrefixExpression(field.Value);
                    }
                    else
                    {
                        return new TypeEqualsExpression(field.Value);
                    }

                case "tag":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
                        return new TagExpression(tags);
                    }

                case "rejected":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
                        return new RejectedTagExpression(tags);
                    }

                case "~":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    return this.CompileConjunction(
                        this.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, field.Value))),
                        this.CompileNegation(this.CompileField(new FieldTerm("rejected", FieldTerm.GreaterThanOrEqualOperator, field.Value))));

                case "suggested":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        return this.CompileDisjunction(
                            tags.Select(tag => this.CompileTagRelation(TagOperator.Suggestion, tag)));
                    }

                case "missing":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        return this.CompileDisjunction(
                            tags.Select(tag => this.CompileTagRelation(TagOperator.Implication, tag)));
                    }

                case "add":
                    return this.CompileDisjunction(
                        this.CompileField(new FieldTerm("missing", field.Operator, field.Value)),
                        this.CompileField(new FieldTerm("suggested", field.Operator, field.Value)));

                case "*":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    return this.CompileDisjunction(
                        this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, field.Value)),
                        this.CompileField(new FieldTerm("missing", FieldTerm.LessThanOrEqualOperator, field.Value)),
                        this.CompileField(new FieldTerm("suggested", FieldTerm.LessThanOrEqualOperator, field.Value)));

                case "similar":
                    {
                        if (field.Operator != FieldTerm.EqualsOperator)
                        {
                            throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                        }

                        var visualHash = Convert.ToUInt64(field.Value, 16);
                        return this.CompileDisjunction(
                            AverageIntensityHash.Expand(visualHash, mode: 1).Select(h => new DetailsExpression(ImageDetailRecognizer.Properties.AverageIntensityHash, FieldTerm.EqualsOperator, (long)h)));
                    }

                case "copies":
                    if (!int.TryParse(field.Value, out var copies))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new CopiesExpression(field.Operator, copies);

                case "tags":
                    if (!int.TryParse(field.Value, out var tagCount))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new TagCountExpression(field.Operator, tagCount);

                case "people":
                    if (!int.TryParse(field.Value, out var peopleCount))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new PeopleCountExpression(field.Operator, peopleCount);

                case "rating":
                    if (!double.TryParse(field.Value, out var rating))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new RatingExpression(field.Operator, rating);

                case "ratings":
                    if (!int.TryParse(field.Value, out var ratings))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new RatingsCountExpression(field.Operator, ratings);

                case "stars":
                    if (!int.TryParse(field.Value, out var stars))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }
                    else if (stars < 1 || stars > 5)
                    {
                        throw new NotSupportedException($"Field '{field.Field}' expects a value between 1 and 5.");
                    }

                    Expression StarsExpr(string op, int stars)
                    {
                        switch (op)
                        {
                            case FieldTerm.LessThanOperator:
                                return StarsExpr(FieldTerm.LessThanOrEqualOperator, stars - 1);

                            case FieldTerm.GreaterThanOperator:
                                return StarsExpr(FieldTerm.GreaterThanOrEqualOperator, stars + 1);

                            case FieldTerm.EqualsOperator:
                                var lte = StarsExpr(FieldTerm.LessThanOrEqualOperator, stars);
                                var gte = StarsExpr(FieldTerm.GreaterThanOrEqualOperator, stars);
                                return gte != null && lte != null ? this.CompileConjunction(gte, lte) : gte ?? lte;

                            case FieldTerm.LessThanOrEqualOperator:
                                if (stars >= this.starRanges.Count)
                                {
                                    return null;
                                }
                                else if (stars >= 0 && this.starRanges[stars].min is double min)
                                {
                                    return new RatingExpression(FieldTerm.LessThanOperator, min);
                                }
                                else
                                {
                                    return this.CompileDisjunction();
                                }

                            case FieldTerm.GreaterThanOrEqualOperator:
                                stars -= 1;
                                if (stars >= this.starRanges.Count)
                                {
                                    return this.CompileDisjunction();
                                }
                                else if (stars >= 0 && this.starRanges[stars].min is double min)
                                {
                                    return new RatingExpression(FieldTerm.GreaterThanOrEqualOperator, min);
                                }
                                else
                                {
                                    return null;
                                }

                            default:
                                throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                        }
                    }

                    return StarsExpr(field.Operator, stars) ?? this.CompileConjunction();

                case "size":
                    // TODO: Parse filesizes.
                    if (!long.TryParse(field.Value, out var fileSize))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new FileSizeExpression(field.Operator, fileSize);

                case "duration":
                case "length":
                case "time":
                    {
                        if (!double.TryParse(field.Value, out var seconds))
                        {
                            if (TimeSpan.TryParse(field.Value, out var timeSpan))
                            {
                                seconds = timeSpan.TotalSeconds;
                            }
                            else
                            {
                                throw new NotSupportedException($"Cannot use non-numeric, non-timespan value '{field.Value}' with field '{field.Field}'.");
                            }
                        }

                        return new DetailsExpression(ImageDetailRecognizer.Properties.Duration, field.Operator, seconds);
                    }

                case "width":
                    if (!long.TryParse(field.Value, out var width))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new DetailsExpression(ImageDetailRecognizer.Properties.Width, field.Operator, width);

                case "height":
                    if (!long.TryParse(field.Value, out var height))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new DetailsExpression(ImageDetailRecognizer.Properties.Height, field.Operator, height);

                case "hash":
                    {
                        var value = field.Value.ToLowerInvariant();
                        if (value.StartsWith("0x"))
                        {
                            value = value.Substring("0x".Length);
                        }

                        return new HashExpression(field.Operator, value);
                    }

                case "percent":
                    if (!double.TryParse(field.Value, out var percent))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new SampleExpression(Math.Clamp(percent / 100.0, 0, 1));

                case "sample":
                    if (!double.TryParse(field.Value, out var sample))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return new SampleExpression(Math.Clamp(sample, 0, 1));

                default:
                    throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override Expression CompileNegation(Expression query) =>
            new NegationExpression(query);

        /// <inheritdoc/>
        public override Expression CompilePropertyConjunction(PropertyConjunctionTerm propertyConjunction)
        {
            var engine = this.TagEngine;

            var predicates = propertyConjunction.Predicates.Select(p => this.CompilePropertyPredicate(p));

            var tags = (from tag in engine.GetKnownTags(canonicalOnly: true)
                        let tagInfo = engine[tag]
                        where predicates.All(predicate => tagInfo.Properties.Any(p => predicate(p)))
                        from related in tagInfo.RelatedTags(HierarchyRelation.SelfOrDescendant)
                        select related).ToImmutableHashSet<string>();
            tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
            return new TagExpression(tags);
        }

        private Predicate<string> CompilePropertyPredicate(PropertyPredicate property)
        {
            var f = property.Field;
            if (f.Contains("="))
            {
                return _ => false;
            }

            var fs = $"{f}=";

            switch (property.Operator)
            {
                case null:
                case FieldTerm.EqualsOperator when string.IsNullOrEmpty(property.Value):
                    return prop => prop == f || prop.StartsWith(fs, StringComparison.Ordinal);

                case FieldTerm.EqualsOperator:
                    return prop =>
                    {
                        if (prop.StartsWith(fs, StringComparison.Ordinal))
                        {
                            var value = prop.Substring(fs.Length);
                            return value.IndexOf(property.Value, StringComparison.CurrentCultureIgnoreCase) > -1;
                        }

                        return false;
                    };

                default:
                    throw new NotImplementedException($"Cannot use operator '{property.Operator}' for tag properties.");
            }
        }

        private Expression CompileTagRelation(TagOperator @operator, string tag)
        {
            var tagEngine = this.TagEngine;
            var tagInfo = tagEngine[tag];
            var searchTags = tagInfo.RelatedTags(HierarchyRelation.SelfOrDescendant);
            var exclusionTags = tagInfo.RelatedTags(HierarchyRelation.SelfOrAncestor);
            var rules = tagEngine[@operator].Where(rule => rule.Right.Contains(tag));
            var exclusions = tagEngine[TagOperator.Exclusion].Where(rule => rule.Right.Any(r => exclusionTags.Contains(r)));
            return this.CompileConjunction(
                this.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, tag))),
                this.CompileNegation(this.CompileField(new FieldTerm("rejected", FieldTerm.GreaterThanOrEqualOperator, tag))),
                this.CompileDisjunction(rules.Select(rule =>
                {
                    var requirements = Enumerable.Concat(
                        rule.Left.Select(required => this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, required))),
                        rule.Right.Where(r => !searchTags.Contains(r)).Select(sufficient => this.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, sufficient)))));
                    return this.CompileConjunction(requirements);
                })),
                this.CompileConjunction(exclusions.Select(rule =>
                {
                    var requirements = Enumerable.Concat(
                        rule.Left.Select(required => this.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, required)))),
                        rule.Right.Where(r => !exclusionTags.Contains(r)).Select(excluded => this.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, excluded)))));
                    return this.CompileDisjunction(requirements);
                })));
        }

        private class ContainsHiddenReplacer : ExpressionReplacer<bool>
        {
            private readonly ImmutableHashSet<string> hiddenTags;

            public ContainsHiddenReplacer(TagRuleEngine tagEngine)
            {
                this.hiddenTags = tagEngine.GetTagDescendants(HiddenTag).Add(HiddenTag);
            }

            public override bool Replace(ConjunctionExpression expression) => expression.Expressions.Select(this.Replace).Any(c => c);

            public override bool Replace(DisjunctionExpression expression) => expression.Expressions.Select(this.Replace).Any(c => c);

            public override bool Replace(NegationExpression expression) => this.Replace(expression.Expression);

            public override bool Replace(CopiesExpression expression) => false;

            public override bool Replace(DetailsExpression expression) => false;

            public override bool Replace(FileSizeExpression expression) => false;

            public override bool Replace(HashExpression expression) => false;

            public override bool Replace(SampleExpression expression) => false;

            public override bool Replace(NoPeopleExpression expression) => false;

            public override bool Replace(PeopleCountExpression expression) => false;

            public override bool Replace(PersonIdExpression expression) => false;

            public override bool Replace(PersonNameExpression expression) => false;

            public override bool Replace(RatingExpression expression) => false;

            public override bool Replace(RatingsCountExpression expression) => false;

            public override bool Replace(TagExpression expression) => expression.Tags.Overlaps(this.hiddenTags);

            public override bool Replace(RejectedTagExpression expression) => expression.Tags.Overlaps(this.hiddenTags);

            public override bool Replace(TagCountExpression expression) => false;

            public override bool Replace(TextExpression expression) => false;

            public override bool Replace(TypeEqualsExpression expression) => false;

            public override bool Replace(TypePrefixExpression expression) => false;
        }
    }
}
