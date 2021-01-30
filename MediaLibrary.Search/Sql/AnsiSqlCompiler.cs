// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using static MediaLibrary.Search.Sql.QueryBuilder;

    public abstract class AnsiSqlCompiler : QueryCompiler<string>
    {
        public AnsiSqlCompiler(Func<string, Term> getSavedSearch)
            : base(getSavedSearch)
        {
        }

        public static string Contains(string expr, string patternValue)
        {
            string literal;
            char? escape;
            if (patternValue.IndexOfAny(new[] { '%', '_' }) > -1)
            {
                literal = Literal('%' + EscapeLike(patternValue) + '%');
                escape = '\\';
            }
            else
            {
                literal = Literal('%' + patternValue + '%');
                escape = null;
            }

            return Like(expr, literal, escape);
        }

        public static string ConvertOperator(string fieldOperator)
        {
            switch (fieldOperator)
            {
                case FieldTerm.EqualsOperator:
                    return "=";

                case FieldTerm.GreaterThanOperator:
                case FieldTerm.GreaterThanOrEqualOperator:
                case FieldTerm.LessThanOperator:
                case FieldTerm.LessThanOrEqualOperator:
                    return fieldOperator;

                default:
                    throw new NotSupportedException($"Unrecognized operator '{fieldOperator}'.");
            }
        }

        public static string Like(string expr, string patternExpr, char? escape)
        {
            var sb = new StringBuilder()
                .Append(expr)
                .Append(" LIKE ")
                .Append(patternExpr);

            if (escape != null)
            {
                sb
                    .Append(" ESCAPE ")
                    .Append(Literal(escape.Value));
            }

            return sb.ToString();
        }

        public static string StartsWith(string expr, string patternExpr, char? escape) => Like(expr, patternExpr, escape);

        public static string StartsWith(string expr, string patternValue)
        {
            string literal;
            char? escape;
            if (patternValue.IndexOfAny(new[] { '%', '_' }) > -1)
            {
                literal = Literal(EscapeLike(patternValue) + '%');
                escape = '\\';
            }
            else
            {
                literal = Literal(patternValue + '%');
                escape = null;
            }

            return Like(expr, literal, escape);
        }

        /// <inheritdoc/>
        public override string CompileConjunction(IEnumerable<string> query)
        {
            var sb = new StringBuilder()
                .Append("(");

            var first = true;
            foreach (var term in query)
            {
                if (!first)
                {
                    sb.Append(") AND (");
                }

                sb.Append(term);
                first = false;
            }

            if (first)
            {
                sb.Append("1 = 1");
            }

            return sb.Append(")").ToString();
        }

        /// <inheritdoc/>
        public override string CompileDisjunction(IEnumerable<string> query)
        {
            var sb = new StringBuilder()
                .Append("(");

            var first = true;
            foreach (var term in query)
            {
                if (!first)
                {
                    sb.Append(") OR (");
                }

                sb.Append(term);
                first = false;
            }

            if (first)
            {
                sb.Append("1 = 0");
            }

            return sb.Append(")").ToString();
        }

        /// <inheritdoc/>
        public override string CompileNegation(string query)
        {
            return $"NOT ({query})";
        }
    }
}
