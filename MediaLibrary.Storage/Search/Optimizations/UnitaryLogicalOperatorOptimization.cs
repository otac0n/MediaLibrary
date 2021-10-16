// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class UnitaryLogicalOperatorOptimization : Optimization
    {
        public override Expression Replace(ConjunctionExpression expression)
        {
            if (expression.Expressions.Count == 1)
            {
                return this.Replace(expression.Expressions.Single());
            }

            return base.Replace(expression);
        }

        public override Expression Replace(DisjunctionExpression expression)
        {
            if (expression.Expressions.Count == 1)
            {
                return this.Replace(expression.Expressions.Single());
            }

            return base.Replace(expression);
        }
    }
}
