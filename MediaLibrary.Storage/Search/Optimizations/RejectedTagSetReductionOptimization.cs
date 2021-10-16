// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Storage.Search.Expressions;

    public sealed class RejectedTagSetReductionOptimization : Optimization
    {
        public override Expression Replace(DisjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is NegationExpression n && n.Expression is RejectedTagExpression);
            var tagSetCount = lookup[true].Count();
            if (tagSetCount >= 2)
            {
                var reducedSet = lookup[true].Cast<NegationExpression>()
                    .OrderByDescending(n => ((RejectedTagExpression)n.Expression).Tags.Count)
                    .Aggregate(
                        ImmutableList<NegationExpression>.Empty,
                        (l, e) => !l.Any(t => ((RejectedTagExpression)t.Expression).Tags.IsSupersetOf(((RejectedTagExpression)e.Expression).Tags)) ? l.Add(e) : l);

                if (reducedSet.Count < tagSetCount)
                {
                    expression = new DisjunctionExpression(lookup[false].Concat(reducedSet).ToImmutableList());
                }
            }

            return base.Replace(expression);
        }

        public override Expression Replace(ConjunctionExpression expression)
        {
            var lookup = expression.Expressions.ToLookup(e => e is RejectedTagExpression);
            var tagSetCount = lookup[true].Count();
            if (tagSetCount >= 2)
            {
                var reducedSet = lookup[true].Cast<RejectedTagExpression>()
                    .OrderBy(t => t.Tags.Count)
                    .Aggregate(
                        ImmutableList<RejectedTagExpression>.Empty,
                        (l, e) => !l.Any(t => t.Tags.IsSubsetOf(e.Tags)) ? l.Add(e) : l);

                if (reducedSet.Count < tagSetCount)
                {
                    expression = new ConjunctionExpression(lookup[false].Concat(reducedSet).ToImmutableList());
                }
            }

            return base.Replace(expression);
        }
    }
}
