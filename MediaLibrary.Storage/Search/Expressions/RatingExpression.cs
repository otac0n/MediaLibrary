// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class RatingExpression : Expression
    {
        public RatingExpression(string @operator, double rating)
        {
            this.Operator = @operator;
            this.Rating = rating;
        }

        public string Operator { get; }

        public double Rating { get; }
    }
}
