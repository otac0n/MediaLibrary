// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class DeMorgansOptimization : Optimization
    {
        public override Expression Replace(DisjunctionExpression expression)
        {
            if (expression.Expressions.Count > 0 && expression.Expressions.All(e => e is NegationExpression))
            {
                var inners = expression.Expressions.Cast<NegationExpression>().Select(n => this.Replace(n.Expression)).ToImmutableList();

                return new NegationExpression(new ConjunctionExpression(inners));
            }

            return base.Replace(expression);
        }

        public override Expression Replace(ConjunctionExpression expression)
        {
            if (expression.Expressions.Count > 0 && expression.Expressions.All(e => e is NegationExpression))
            {
                var inners = expression.Expressions.Cast<NegationExpression>().Select(n => this.Replace(n.Expression)).ToImmutableList();

                return new NegationExpression(new DisjunctionExpression(inners));
            }

            return base.Replace(expression);
        }
    }
}
