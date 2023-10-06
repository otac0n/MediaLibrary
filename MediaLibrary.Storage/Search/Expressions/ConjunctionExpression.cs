// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    using System.Collections.Immutable;

    public sealed class ConjunctionExpression : Expression
    {
        public ConjunctionExpression(ImmutableList<Expression> expressions)
        {
            this.Expressions = expressions;
        }

        public ImmutableList<Expression> Expressions { get; }
    }
}
