// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    using System;

    public class FieldTerm : Term
    {
        public static readonly int Precedence = 3;

        public FieldTerm(string field, string value)
        {
            this.Field = field;
            this.Value = value;
        }

        public string Field { get; }

        public string Value { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var valueEscaped = "\"" + this.Value.Replace("\"", "\"\"") + "\"";
            switch (this.Field)
            {
                case null:
                    return valueEscaped;

                case "tag":
                    return $"#{valueEscaped}";

                case "@":
                    return $"@{valueEscaped}";

                default:
                    return $"\"{this.Field.Replace("\"", "\"\"")}\":{valueEscaped}";
            }
        }
    }
}
