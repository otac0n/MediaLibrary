// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System.Collections.Immutable;
    using MediaLibrary.Storage.Search.Expressions;

    public abstract class Optimization : ExpressionReplacer<Expression>
    {
        public override Expression Replace(ConjunctionExpression expression)
        {
            Expression[] allReplaced = null;
            this.ReplaceChildren(expression.Expressions, ref allReplaced);

            if (allReplaced != null)
            {
                expression = new ConjunctionExpression(ImmutableList.Create(allReplaced));
            }

            return expression;
        }

        public override Expression Replace(DisjunctionExpression expression)
        {
            Expression[] allReplaced = null;
            this.ReplaceChildren(expression.Expressions, ref allReplaced);

            if (allReplaced != null)
            {
                expression = new DisjunctionExpression(ImmutableList.Create(allReplaced));
            }

            return expression;
        }

        public override Expression Replace(NegationExpression expression)
        {
            var replaced = this.Replace(expression.Expression);
            if (!object.ReferenceEquals(expression.Expression, replaced))
            {
                return new NegationExpression(replaced);
            }

            return expression;
        }

        public override Expression Replace(CopiesExpression expression) => expression;

        public override Expression Replace(DetailsExpression expression) => expression;

        public override Expression Replace(FileSizeExpression expression) => expression;

        public override Expression Replace(HashExpression expression) => expression;

        public override Expression Replace(SampleExpression expression) => expression;

        public override Expression Replace(NoPeopleExpression expression) => expression;

        public override Expression Replace(PeopleCountExpression expression) => expression;

        public override Expression Replace(PersonIdExpression expression) => expression;

        public override Expression Replace(PersonNameExpression expression) => expression;

        public override Expression Replace(RatingExpression expression) => expression;

        public override Expression Replace(RatingsCountExpression expression) => expression;

        public override Expression Replace(StarsExpression expression) => expression;

        public override Expression Replace(TagExpression expression) => expression;

        public override Expression Replace(RejectedTagExpression expression) => expression;

        public override Expression Replace(TagCountExpression expression) => expression;

        public override Expression Replace(TextExpression expression) => expression;

        public override Expression Replace(TypeEqualsExpression expression) => expression;

        public override Expression Replace(TypePrefixExpression expression) => expression;

        private void ReplaceChildren(ImmutableList<Expression> expressions, ref Expression[] allReplaced)
        {
            var count = expressions.Count;
            for (var i = 0; i < count; i++)
            {
                var original = expressions[i];
                var replaced = this.Replace(original);

                if (allReplaced != null || !object.ReferenceEquals(original, replaced))
                {
                    if (allReplaced == null)
                    {
                        allReplaced = new Expression[count];
                        expressions.CopyTo(0, allReplaced, 0, i);
                    }

                    allReplaced[i] = replaced;
                }
            }
        }
    }
}
