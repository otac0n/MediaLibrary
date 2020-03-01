// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    public class SearchDialect : AnsiSqlCompiler
    {
        private int depth = 0;
        private bool joinTags = false;

        /// <inheritdoc/>
        public override string Compile(Term term)
        {
            var originalDepth = this.depth;
            this.depth++;
            try
            {
                if (originalDepth == 0)
                {
                    this.joinTags = false;
                    return this.FinalizeQuery(base.Compile(term));
                }
                else
                {
                    return base.Compile(term);
                }
            }
            finally
            {
                this.depth = originalDepth;
            }
        }

        /// <inheritdoc/>
        public override string CompileField(FieldTerm field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            switch (field.Field)
            {
                case "type":
                    var ix = field.Value.IndexOf('/');
                    return
                        ix < 0 ? $"FileType = {Literal(field.Value)} OR {StartsWith("FileType", field.Value + "/")}" :
                        ix == field.Value.Length - 1 ? StartsWith("FileType", field.Value) :
                        $"FileType = {Literal(field.Value)}";

                case "tag":
                    this.joinTags = true;
                    return $"Tag = {Literal(field.Value)}";

                default:
                    throw new NotSupportedException();
            }
        }

        private static string Like(string expr, string patternExpr, char? escape)
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

        private static string Literal(string value) => $"'{value.Replace("'", "''")}'";

        private static string Literal(char value) => Literal(value.ToString());

        private static string Literal(int value) => value.ToString();

        private static string Literal(double value) => value.ToString();

        private static string StartsWith(string expr, string patternExpr, char? escape) => Like(expr, patternExpr, escape);

        private static string StartsWith(string expr, string patternValue)
        {
            string literal;
            char? escape;
            if (patternValue.IndexOfAny(new[] { '%', '_' }) > -1)
            {
                literal = Literal(Regex.Replace(patternValue, @"[%_\\]", @"\$0") + '%');
                escape = '\\';
            }
            else
            {
                literal = Literal(patternValue + '%');
                escape = null;
            }

            return Like(expr, literal, escape);
        }

        private string FinalizeQuery(string filter)
        {
            var fetchTags = true;
            var fetchPaths = true;
            var fetchAny = fetchTags || fetchPaths;

            var sb = new StringBuilder();

            if (fetchAny)
            {
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.SearchHashInfo;")
                    .AppendLine("CREATE TEMP TABLE temp.SearchHashInfo (Hash text, FileSize integer, FileType text, PRIMARY KEY (Hash));")
                    .AppendLine("INSERT INTO temp.SearchHashInfo (Hash, FileSize, FileType)");
            }

            sb
                .AppendLine("SELECT DISTINCT h.Hash, h.FileSize, h.FileType")
                .AppendLine("FROM HashInfo h");

            if (this.joinTags)
            {
                sb.AppendLine("LEFT JOIN HashTags t ON h.Hash = t.Hash");
            }

            sb
                .AppendLine("WHERE (")
                .AppendLine(filter)
                .AppendLine(");");

            if (fetchTags)
            {
                sb.AppendLine("SELECT t.* FROM temp.SearchHashInfo h INNER JOIN HashTags t ON h.Hash = t.Hash;");
            }

            if (fetchPaths)
            {
                sb.AppendLine("SELECT p.* FROM temp.SearchHashInfo h INNER JOIN Paths p ON h.Hash = p.LastHash;");
            }

            if (fetchAny)
            {
                sb
                    .AppendLine("SELECT * FROM temp.SearchHashInfo;")
                    .AppendLine("DROP TABLE temp.SearchHashInfo;");
            }

            return sb.ToString();
        }
    }
}
