// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Search;
    using MediaLibrary.Tagging;

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
                    var tagInfo = this.TagEngine[field.Value];
                    if (this.ExcludeHidden && (tagInfo.Tag == "hidden" || tagInfo.Ancestors.Contains("hidden")))
                    {
                        this.ExcludeHidden = false;
                    }

                    var tags = ImmutableHashSet<string>.Empty;
                    switch (field.Operator)
                    {
                        case FieldTerm.GreaterThanOperator:
                        case FieldTerm.GreaterThanOrEqualOperator:

                            tags = tags.Union(tagInfo.Ancestors);

                            if (field.Operator == FieldTerm.GreaterThanOrEqualOperator)
                            {
                                goto case FieldTerm.EqualsOperator;
                            }

                            break;

                        case FieldTerm.LessThanOperator:
                        case FieldTerm.LessThanOrEqualOperator:

                            tags = tags.Union(tagInfo.Descendants);

                            if (field.Operator == FieldTerm.LessThanOrEqualOperator)
                            {
                                goto case FieldTerm.EqualsOperator;
                            }

                            break;

                        case FieldTerm.EqualsOperator:
                            tags = tags.Add(tagInfo.Tag);
                            break;
                    }

                    tags = tags.Union(tags.SelectMany(this.TagEngine.GetTagAliases));
                    return this.Tag(tags);

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

                case "hash":
                    return this.Hash(field.Operator, field.Value);

                default:
                    throw new NotSupportedException();
            }
        }

        public abstract T Copies(string @operator, int value);

        public abstract T Hash(string @operator, string value);

        public virtual T NoPerson() => this.PersonCount(FieldTerm.EqualsOperator, 0);

        public abstract T PersonCount(string @operator, int value);

        public abstract T PersonId(int value);

        public abstract T PersonName(string value);

        public abstract T Rating(string @operator, double value);

        public abstract T Stars(string @operator, int value);

        public abstract T Tag(ImmutableHashSet<string> value);

        public abstract T TagCount(string @operator, int value);

        public abstract T TextSearch(string value);

        public abstract T TypeEquals(string value);

        public abstract T TypePrefixed(string value);
    }
}
