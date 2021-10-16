// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class RatingsCountExpression : Expression
    {
        public RatingsCountExpression(string @operator, int ratings)
        {
            this.Operator = @operator;
            this.RatingsCount = ratings;
        }

        public string Operator { get; }

        public int RatingsCount { get; }
    }
}
