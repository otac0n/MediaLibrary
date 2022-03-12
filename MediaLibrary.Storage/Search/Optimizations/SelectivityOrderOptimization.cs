// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public class SelectivityOrderOptimization : Optimization
    {
        private SelectivityEstimator selectivityEstimator = new SelectivityEstimator();

        public override Expression Replace(ConjunctionExpression expression)
        {
            var expressions = expression.Expressions;

            Expression[] allReplaced = null;
            var count = expressions.Count;
            var previousSelectivity = double.NegativeInfinity;
            for (var i = 0; i < count; i++)
            {
                var original = expressions[i];
                var replaced = this.Replace(original);
                var selectivity = this.selectivityEstimator.Replace(replaced);

                if (allReplaced != null || !object.ReferenceEquals(original, replaced) || selectivity < previousSelectivity)
                {
                    if (allReplaced == null)
                    {
                        allReplaced = new Expression[count];
                        expressions.CopyTo(0, allReplaced, 0, i);
                    }

                    allReplaced[i] = replaced;
                }

                previousSelectivity = selectivity;
            }

            if (allReplaced != null)
            {
                expression = new ConjunctionExpression(ImmutableList.CreateRange(allReplaced.OrderBy(this.selectivityEstimator.Replace)));
            }

            return expression;
        }

        public override Expression Replace(DisjunctionExpression expression)
        {
            var expressions = expression.Expressions;

            Expression[] allReplaced = null;
            var count = expressions.Count;
            var previousSelectivity = double.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var original = expressions[i];
                var replaced = this.Replace(original);
                var selectivity = this.selectivityEstimator.Replace(replaced);

                if (allReplaced != null || !object.ReferenceEquals(original, replaced) || selectivity > previousSelectivity)
                {
                    if (allReplaced == null)
                    {
                        allReplaced = new Expression[count];
                        expressions.CopyTo(0, allReplaced, 0, i);
                    }

                    allReplaced[i] = replaced;
                }

                previousSelectivity = selectivity;
            }

            if (allReplaced != null)
            {
                expression = new DisjunctionExpression(ImmutableList.CreateRange(allReplaced.OrderByDescending(this.selectivityEstimator.Replace)));
            }

            return expression;
        }
    }
}
