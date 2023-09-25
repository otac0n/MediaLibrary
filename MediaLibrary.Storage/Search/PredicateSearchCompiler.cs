// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Search.Terms;
    using MediaLibrary.Storage.Search.Expressions;

    public class PredicateSearchCompiler : SearchCompiler<Predicate<SearchResult>>
    {
        private static readonly PredicateReplacer ReplacerInstance = new PredicateReplacer();

        protected override Predicate<SearchResult> Compile(Expression expression) => ReplacerInstance.Replace(expression);

        private class PredicateReplacer : ExpressionReplacer<Predicate<SearchResult>>
        {
            public static Func<int, bool> ConvertOperator(string fieldOperator)
            {
                switch (fieldOperator)
                {
                    case FieldTerm.EqualsOperator:
                        return x => x == 0;

                    case FieldTerm.UnequalOperator:
                        return x => x != 0;

                    case FieldTerm.ComparableOperator:
                        return x => true;

                    case FieldTerm.GreaterThanOperator:
                        return x => x > 0;

                    case FieldTerm.GreaterThanOrEqualOperator:
                        return x => x >= 0;

                    case FieldTerm.LessThanOperator:
                        return x => x < 0;

                    case FieldTerm.LessThanOrEqualOperator:
                        return x => x <= 0;

                    default:
                        throw new NotSupportedException($"Unrecognized operator '{fieldOperator}'.");
                }
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(DetailsExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                throw new NotImplementedException("Query hash details table and compare to this result.");
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(PersonIdExpression expression) => x => x.People.Any(p => p.PersonId == expression.PersonId);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(PersonNameExpression expression) => x => x.People.Any(p => p.Name.IndexOf(expression.Value, StringComparison.CurrentCultureIgnoreCase) > -1);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(RejectedTagExpression expression) => x => x.RejectedTags.Overlaps(expression.Tags);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(FileSizeExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op(x.FileSize.CompareTo(expression.FileSize));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(HashExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op(StringComparer.InvariantCultureIgnoreCase.Compare(x.Hash, expression.Value));
            }

            public override Predicate<SearchResult> Replace(SampleExpression expression) =>
                x => Random.Shared.NextDouble() < expression.Portion;

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(PeopleCountExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op(x.People.Count.CompareTo(expression.PeopleCount));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(NoPeopleExpression expression) => this.Replace(new PeopleCountExpression(FieldTerm.EqualsOperator, 0));

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(RatingExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op((x.Rating?.Value ?? Storage.Rating.DefaultRating).CompareTo(expression.Rating));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(RatingsCountExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op((x.Rating?.Count ?? 0).CompareTo(expression.RatingsCount));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(CopiesExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op(x.Paths.Count.CompareTo(expression.Copies));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(ConjunctionExpression expression)
            {
                var predicates = expression.Expressions.Select(this.Replace).ToList();
                return t => predicates.All(p => p(t));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(DisjunctionExpression expression)
            {
                var predicates = expression.Expressions.Select(this.Replace).ToList();
                return t => predicates.Any(p => p(t));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(NegationExpression expression)
            {
                var query = this.Replace(expression.Expression);
                return x => !query(x);
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(TextExpression expression) => x => x.Paths.Any(p => p.IndexOf(expression.Value, StringComparison.CurrentCultureIgnoreCase) > 0);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(StarsExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                throw new NotImplementedException("Get stars statistics from the database and compare to the percentiles.");
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(TagExpression expression) => x => x.Tags.Overlaps(expression.Tags);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(TagCountExpression expression)
            {
                var op = ConvertOperator(expression.Operator);
                return x => op(x.Tags.Count.CompareTo(expression.TagCount));
            }

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(TypeEqualsExpression expression) => x => x.FileType.Equals(expression.Value, StringComparison.OrdinalIgnoreCase);

            /// <inheritdoc/>
            public override Predicate<SearchResult> Replace(TypePrefixExpression expression) => x => x.FileType.StartsWith(expression.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
