// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Search;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search.Expressions;
    using TaggingLibrary;

    public sealed class SearchDialect : QueryCompiler<Expression>
    {
        public SearchDialect(TagRuleEngine tagEngine, Func<string, Term> getSavedSearch)
            : base(getSavedSearch)
        {
            this.TagEngine = tagEngine;
        }

        private TagRuleEngine TagEngine { get; }

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

                    return new StarsExpression(field.Operator, stars);

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

                case "hash":
                    {
                        var value = field.Value.ToLowerInvariant();
                        if (value.StartsWith("0x"))
                        {
                            value = value.Substring("0x".Length);
                        }

                        return new HashExpression(field.Operator, value);
                    }

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
            var rules = tagEngine[@operator].Where(rule => rule.Right.Any(r => searchTags.Contains(r)));
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
    }
}
