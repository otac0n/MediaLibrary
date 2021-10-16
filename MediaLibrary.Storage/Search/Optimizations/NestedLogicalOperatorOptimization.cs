// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class NestedLogicalOperatorOptimization : Optimization
    {
        public override Expression Replace(ConjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is ConjunctionExpression);
            if (lookup[true].Any())
            {
                var expressions = lookup[true].Cast<ConjunctionExpression>().SelectMany(e => e.Expressions);

                var builder = ImmutableList.CreateBuilder<Expression>();
                builder.AddRange(lookup[false]);
                builder.AddRange(expressions);

                expression = new ConjunctionExpression(builder.ToImmutable());
            }

            return base.Replace(expression);
        }

        public override Expression Replace(DisjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is DisjunctionExpression);
            if (lookup[true].Any())
            {
                var expressions = lookup[true].Cast<DisjunctionExpression>().SelectMany(e => e.Expressions);

                var builder = ImmutableList.CreateBuilder<Expression>();
                builder.AddRange(lookup[false]);
                builder.AddRange(expressions);

                expression = new DisjunctionExpression(builder.ToImmutable());
            }

            return base.Replace(expression);
        }

        public override Expression Replace(NegationExpression expression)
        {
            if (expression.Expression is NegationExpression nestedNegation)
            {
                return this.Replace(nestedNegation.Expression);
            }

            return base.Replace(expression);
        }
    }
}
