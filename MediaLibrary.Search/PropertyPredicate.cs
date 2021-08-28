// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    public class PropertyPredicate : Term
    {
        public PropertyPredicate(string field, string @operator, string value)
        {
            this.Field = field;
            this.Operator = @operator;
            this.Value = value;
        }

        public PropertyPredicate(string field)
            : this(field, null, null)
        {
        }

        public string Field { get; }

        public string Operator { get; }

        public string Value { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var fieldQuoted = FieldTerm.QuoteIfNecessary(this.Field);
            if (this.Operator == null)
            {
                return fieldQuoted;
            }
            else
            {
                return $"{fieldQuoted}{this.Operator}{FieldTerm.QuoteIfNecessary(this.Value)}";
            }
        }
    }
}
