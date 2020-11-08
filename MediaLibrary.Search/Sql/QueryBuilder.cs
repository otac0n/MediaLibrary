// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Sql
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    public static class QueryBuilder
    {
        public static string EscapeLike(string patternValue) =>
            Regex.Replace(patternValue, @"[%_\\]", @"\$0");

        public static string Literal(string value) =>
            $"'{value.Replace("'", "''")}'";

        public static string Literal(char value) =>
            Literal(value.ToString(CultureInfo.InvariantCulture));

        public static string Literal(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        public static string Literal(double value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
