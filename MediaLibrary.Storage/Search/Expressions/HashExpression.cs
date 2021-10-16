// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class HashExpression : Expression
    {
        public HashExpression(string @operator, string value)
        {
            this.Operator = @operator;
            this.Value = value;
        }

        public string Operator { get; }

        public string Value { get; }
    }
}
