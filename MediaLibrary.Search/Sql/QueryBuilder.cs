// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Sql
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public static class QueryBuilder
    {
        public static string EscapeLike(string patternValue) =>
            Regex.Replace(patternValue, @"[%_\\]", @"\$0");

        public static string EscapeName(string name) =>
            $"\"{name.Replace("\"", "\"\"")}\"";

        public static string Literal(string value) =>
            $"'{value.Replace("'", "''")}'";

        public static string Literal(char value) =>
            Literal(value.ToString(CultureInfo.InvariantCulture));

        public static string Literal(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(uint value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(ulong value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(double value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(object value)
        {
            switch (value)
            {
                case char @char: return Literal(@char);
                case double @double: return Literal(@double);
                case int @int: return Literal(@int);
                case uint @uint: return Literal(@uint);
                case ulong @ulong: return Literal(@ulong);
                case long @long: return Literal(@long);
                case string @string: return Literal(@string);
            }

            throw new NotSupportedException();
        }
    }
}
