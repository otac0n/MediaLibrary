// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Search;
    using MediaLibrary.Storage.FileTypes;
    using TaggingLibrary;

    public abstract class SearchDialect<T>
    {
        public SearchDialect(TagRuleEngine tagEngine, bool excludeHidden, QueryCompiler<T> parentCompiler)
        {
            this.TagEngine = tagEngine;
            this.ExcludeHidden = excludeHidden;
            this.ParentCompiler = parentCompiler;
        }

        public bool ExcludeHidden { get; private set; }

        public QueryCompiler<T> ParentCompiler { get; }

        private TagRuleEngine TagEngine { get; }

        public T CompileField(FieldTerm field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            switch (field.Field)
            {
                case null:
                    return this.TextSearch(field.Value);

                case "@":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    if (int.TryParse(field.Value, out var personId))
                    {
                        if (personId == 0)
                        {
                            return this.NoPerson();
                        }
                        else
                        {
                            return this.PersonId(personId);
                        }
                    }
                    else
                    {
                        return this.PersonName(field.Value);
                    }

                case "type":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    var ix = field.Value.IndexOf('/');
                    if (ix < 0)
                    {
                        return this.ParentCompiler.CompileDisjunction(new[]
                        {
                            this.TypeEquals(field.Value),
                            this.TypePrefixed(field.Value + "/"),
                        });
                    }
                    else if (ix == field.Value.Length - 1)
                    {
                        return this.TypePrefixed(field.Value);
                    }
                    else
                    {
                        return this.TypeEquals(field.Value);
                    }

                case "tag":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        if (this.ExcludeHidden && (tagInfo.Tag == "hidden" || tagInfo.Ancestors.Contains("hidden")))
                        {
                            this.ExcludeHidden = false;
                        }

                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
                        return this.Tag(tags);
                    }

                case "rejected":
                    {
                        var tagInfo = this.TagEngine[field.Value];
                        var tags = tagInfo.RelatedTags(TagDialect.TagRelationships[field.Operator]);
                        tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
                        return this.RejectedTag(tags);
                    }

                case "~":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    return this.ParentCompiler.CompileConjunction(
                        this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, field.Value))),
                        this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("rejected", FieldTerm.GreaterThanOrEqualOperator, field.Value))));

                case "?":
                case "suggested":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    return this.CompileTagRelation(TagOperator.Suggestion, field.Value);

                case "^":
                case "missing":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    return this.CompileTagRelation(TagOperator.Implication, field.Value);

                case "similar":
                    {
                        if (field.Operator != FieldTerm.EqualsOperator)
                        {
                            throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                        }

                        var visualHash = Convert.ToUInt64(field.Value, 16);
                        return this.ParentCompiler.CompileDisjunction(
                            AverageIntensityHash.Expand(visualHash, mode: 1).Select(h => this.Details(ImageDetailRecognizer.Properties.AverageIntensityHash, FieldTerm.EqualsOperator, (long)h)));
                    }

                case "copies":
                    if (!int.TryParse(field.Value, out var copies))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.Copies(field.Operator, copies);

                case "tags":
                    if (!int.TryParse(field.Value, out var tagCount))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.TagCount(field.Operator, tagCount);

                case "people":
                    if (!int.TryParse(field.Value, out var peopleCount))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.PersonCount(field.Operator, peopleCount);

                case "rating":
                    if (!double.TryParse(field.Value, out var rating))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.Rating(field.Operator, rating);

                case "ratings":
                    if (!int.TryParse(field.Value, out var ratings))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.RatingsCount(field.Operator, ratings);

                case "stars":
                    if (!int.TryParse(field.Value, out var stars))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }
                    else if (stars < 1 || stars > 5)
                    {
                        throw new NotSupportedException($"Field '{field.Field}' expects a value between 1 and 5.");
                    }

                    return this.Stars(field.Operator, stars);

                case "size":
                    // TODO: Parse filesizes.
                    if (!long.TryParse(field.Value, out var fileSize))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return this.FileSize(field.Operator, fileSize);

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

                        return this.Details(ImageDetailRecognizer.Properties.Duration, field.Operator, seconds);
                    }

                case "hash":
                    return this.Hash(field.Operator, field.Value);

                default:
                    throw new NotSupportedException();
            }
        }

        public T CompilePropertyConjunction(PropertyConjunctionTerm propertyConjunction)
        {
            var engine = this.TagEngine;

            var predicates = propertyConjunction.Predicates.Select(p => this.CompilePropertyPredicate(p));

            var tags = (from tag in engine.GetKnownTags(canonicalOnly: true)
                        let tagInfo = engine[tag]
                        where predicates.All(predicate => tagInfo.Properties.Any(p => predicate(p)))
                        from related in tagInfo.RelatedTags(HierarchyRelation.SelfOrDescendant)
                        select related).ToImmutableHashSet<string>();
            tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
            return this.Tag(tags);
        }

        public Predicate<string> CompilePropertyPredicate(PropertyPredicate property)
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

        public abstract T Copies(string @operator, int value);

        public abstract T Details(string detailsField, string @operator, object value);

        public abstract T FileSize(string @operator, long value);

        public abstract T Hash(string @operator, string value);

        public virtual T NoPerson() => this.PersonCount(FieldTerm.EqualsOperator, 0);

        public abstract T PersonCount(string @operator, int value);

        public abstract T PersonId(int value);

        public abstract T PersonName(string value);

        public abstract T Rating(string @operator, double value);

        public abstract T RatingsCount(string @operator, int value);

        public abstract T RejectedTag(ImmutableHashSet<string> value);

        public abstract T Stars(string @operator, int value);

        public abstract T Tag(ImmutableHashSet<string> value);

        public abstract T TagCount(string @operator, int value);

        public abstract T TextSearch(string value);

        public abstract T TypeEquals(string value);

        public abstract T TypePrefixed(string value);

        private T CompileTagRelation(TagOperator @operator, string tag)
        {
            var tagEngine = this.TagEngine;
            var tagInfo = tagEngine[tag];
            var searchTags = tagInfo.RelatedTags(HierarchyRelation.SelfOrDescendant);
            var exclusionTags = tagInfo.RelatedTags(HierarchyRelation.SelfOrAncestor);
            var rules = tagEngine[@operator].Where(rule => rule.Right.Any(r => searchTags.Contains(r)));
            var exclusions = tagEngine[TagOperator.Exclusion].Where(rule => rule.Right.Any(r => exclusionTags.Contains(r)));
            return this.ParentCompiler.CompileConjunction(
                this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, tag))),
                this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("rejected", FieldTerm.GreaterThanOrEqualOperator, tag))),
                this.ParentCompiler.CompileDisjunction(rules.Select(rule =>
                {
                    var requirements = Enumerable.Concat(
                        rule.Left.Select(required => this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, required))),
                        rule.Right.Where(r => !searchTags.Contains(r)).Select(sufficient => this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, sufficient)))));
                    return this.ParentCompiler.CompileConjunction(requirements);
                })),
                this.ParentCompiler.CompileConjunction(exclusions.Select(rule =>
                {
                    var requirements = Enumerable.Concat(
                        rule.Left.Select(required => this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, required)))),
                        rule.Right.Where(r => !exclusionTags.Contains(r)).Select(excluded => this.ParentCompiler.CompileNegation(this.CompileField(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, excluded)))));
                    return this.ParentCompiler.CompileDisjunction(requirements);
                })));
        }
    }
}
