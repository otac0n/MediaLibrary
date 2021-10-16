// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class NegationExpression : Expression
    {
        public NegationExpression(Expression expression)
        {
            this.Expression = expression;
        }

        public Expression Expression { get; }
    }
}
