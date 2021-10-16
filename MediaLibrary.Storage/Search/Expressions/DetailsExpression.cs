// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class DetailsExpression : Expression
    {
        public DetailsExpression(string detailsField, string @operator, object value)
        {
            this.DetailsField = detailsField;
            this.Operator = @operator;
            this.Value = value;
        }

        public string DetailsField { get; }

        public string Operator { get; }

        public object Value { get; }
    }
}
