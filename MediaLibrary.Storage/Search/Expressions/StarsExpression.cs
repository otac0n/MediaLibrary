// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class StarsExpression : Expression
    {
        public StarsExpression(string @operator, int stars)
        {
            this.Operator = @operator;
            this.Stars = stars;
        }

        public string Operator { get; }

        public int Stars { get; }
    }
}
