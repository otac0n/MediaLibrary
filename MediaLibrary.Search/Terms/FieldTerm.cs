// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System.Text.RegularExpressions;

namespace MediaLibrary.Search.Terms
{
    public class FieldTerm : Term
    {
        public const string EqualsOperator = ":";
        public const string GreaterThanOperator = ">";
        public const string GreaterThanOrEqualOperator = ">=";
        public const string LessThanOperator = "<";
        public const string LessThanOrEqualOperator = "<=";
        public const string ComparableOperator = ">=<";
        public const string UnequalOperator = "<>";
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
            var valueEscaped = QuoteIfNecessary(this.Value);

            if (this.Operator == EqualsOperator)
            {
                switch (this.Field)
                {
                    case null:
                        return valueEscaped;

                    case "tag" when this.Operator == LessThanOrEqualOperator:
                        return $"#{valueEscaped}";

                    case "rejected" when this.Operator == GreaterThanOrEqualOperator:
                        return $"!{valueEscaped}";

                    case "suggested" when this.Operator == ComparableOperator:
                        return $"?{valueEscaped}";

                    case "missing" when this.Operator == ComparableOperator:
                        return $"^{valueEscaped}";

                    case "add" when this.Operator == LessThanOrEqualOperator:
                        return $"+{valueEscaped}";

                    case "@" when this.Operator == EqualsOperator:
                    case "~" when this.Operator == EqualsOperator:
                        return $"{this.Field}{valueEscaped}";
                }
            }

            return $"{QuoteIfNecessary(this.Field)}{this.Operator}{valueEscaped}";
        }

        internal static string QuoteIfNecessary(string value)
        {
            return Regex.IsMatch(value, "[a-zA-Z0-9][-a-zA-Z0-9]*")
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
