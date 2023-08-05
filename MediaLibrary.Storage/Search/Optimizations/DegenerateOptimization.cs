// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class DegenerateOptimization : Optimization
    {
        public override Expression Replace(DisjunctionExpression expression)
        {
            var trueExpression = expression.Expressions.FirstOrDefault(e => e is ConjunctionExpression conjunction && conjunction.Expressions.Count == 0);
            if (trueExpression != null)
            {
                return trueExpression;
            }

            return base.Replace(expression);
        }

        public override Expression Replace(ConjunctionExpression expression)
        {
            var falseExpression = expression.Expressions.FirstOrDefault(e => e is DisjunctionExpression conjunction && conjunction.Expressions.Count == 0);
            if (falseExpression != null)
            {
                return falseExpression;
            }

            return base.Replace(expression);
        }
    }
}
