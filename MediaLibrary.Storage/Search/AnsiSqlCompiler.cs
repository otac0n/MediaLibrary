// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System.Collections.Generic;
    using System.Text;

    public abstract class AnsiSqlCompiler : QueryCompiler<string>
    {
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

            return sb.Append(")").ToString();
        }

        /// <inheritdoc/>
        public override string CompileNegation(string query)
        {
            return $"NOT ({query})";
        }
    }
}
