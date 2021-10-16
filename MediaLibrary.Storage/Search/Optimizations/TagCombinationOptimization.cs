// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class TagCombinationOptimization : Optimization
    {
        public override Expression Replace(DisjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is TagExpression);
            if (lookup[true].Count() >= 2)
            {
                var allTags = lookup[true].Cast<TagExpression>().Aggregate(ImmutableHashSet<string>.Empty, (s, e) => s.Union(e.Tags));

                var builder = ImmutableList.CreateBuilder<Expression>();
                builder.AddRange(lookup[false]);
                builder.Add(new TagExpression(allTags));

                expression = new DisjunctionExpression(builder.ToImmutable());
            }

            return base.Replace(expression);
        }

        public override Expression Replace(ConjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is NegationExpression n && n.Expression is TagExpression);
            if (lookup[true].Count() >= 2)
            {
                var allExcludedTags = lookup[true].Cast<NegationExpression>().Aggregate(ImmutableHashSet<string>.Empty, (s, e) => s.Union(((TagExpression)e.Expression).Tags));

                var builder = ImmutableList.CreateBuilder<Expression>();
                builder.AddRange(lookup[false]);
                builder.Add(new NegationExpression(new TagExpression(allExcludedTags)));

                expression = new ConjunctionExpression(builder.ToImmutable());
            }

            return base.Replace(expression);
        }
    }
}
