// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class CopiesExpression : Expression
    {
        public CopiesExpression(string @operator, int copies)
        {
            this.Operator = @operator;
            this.Copies = copies;
        }

        public int Copies { get; }

        public string Operator { get; }
    }
}
