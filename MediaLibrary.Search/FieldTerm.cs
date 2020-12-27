// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    public class FieldTerm : Term
    {
        public const string EqualsOperator = ":";
        public const string GreaterThanOperator = ">";
        public const string GreaterThanOrEqualOperator = ">=";
        public const string LessThanOperator = "<";
        public const string LessThanOrEqualOperator = "<=";
        public static readonly int Precedence = 3;

        public FieldTerm(string field, string @operator, string value)
        {
            this.Field = field;
            this.Operator = @operator;
            this.Value = value;
        }

        public FieldTerm(string field, string value)
            : this(field, EqualsOperator, value)
        {
        }

        public string Field { get; }

        public string Operator { get; }

        public string Value { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var valueEscaped = "\"" + this.Value.Replace("\"", "\"\"") + "\"";

            if (this.Operator == EqualsOperator)
            {
                switch (this.Field)
                {
                    case null:
                        return valueEscaped;

                    case "tag":
                        return $"#{valueEscaped}";

                    case "@":
                        return $"@{valueEscaped}";
                }
            }

            return $"\"{this.Field.Replace("\"", "\"\"")}\"{this.Operator}{valueEscaped}";
        }
    }
}
