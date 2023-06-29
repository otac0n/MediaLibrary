// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using MediaLibrary.Search;
    using MediaLibrary.Storage.Search.Expressions;
    using MediaLibrary.Storage.Search.Optimizations;
    using TaggingLibrary;
    using static MediaLibrary.Storage.Search.QueryBuilder;

    public class SqlSearchCompiler : SearchCompiler<string>
    {
        public SqlSearchCompiler(TagRuleEngine tagEngine, bool excludeHidden, Func<string, Term> getSavedSearch)
            : base(tagEngine, excludeHidden, getSavedSearch)
        {
        }

        protected override string Compile(Expression expression)
        {
            var fetchTags = true;
            var fetchPaths = true;
            var fetchPeople = true;
            var fetchAliases = true && fetchPeople;
            var fetchRatings = true;
            var fetchDetails = true;
            var fetchAny = fetchTags || fetchPaths || fetchPeople || fetchRatings || fetchDetails;

            var selectivityOptimization = new SelectivityOrderOptimization();
            var replacer = new SqlReplacer();
            expression = selectivityOptimization.Replace(expression);
            var filter = replacer.Replace(expression);

            var sb = new StringBuilder();

            if (replacer.MaterializedTags.Count > 0)
            {
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.MaterializedTags;")
                    .AppendLine("CREATE TABLE temp.MaterializedTags (SetIndex integer, Tag text);")
                    .AppendLine("INSERT INTO temp.MaterializedTags VALUES");

                var first = true;
                for (var i = 0; i < replacer.MaterializedTags.Count; i++)
                {
                    var tags = replacer.MaterializedTags[i];
                    foreach (var tag in tags)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.AppendLine(",");
                        }

                        sb.Append("(").Append(i).Append(", ").Append(Literal(tag)).Append(")");
                    }
                }

                sb.AppendLine(";");
            }

            if (fetchAny)
            {
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.SearchHashInfo;")
                    .AppendLine("CREATE TEMP TABLE temp.SearchHashInfo (Hash text, FileSize integer, FileType text, Version integer, PRIMARY KEY (Hash));")
                    .AppendLine("INSERT INTO temp.SearchHashInfo (Hash, FileSize, FileType, Version)");
            }

            sb
                .AppendLine("SELECT h.Hash, h.FileSize, h.FileType, h.Version")
                .AppendLine("FROM HashInfo h");

            if (replacer.JoinCopies)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT LastHash Hash, COUNT(*) Copies")
                    .AppendLine("    FROM Paths")
                    .AppendLine("    WHERE MissingSince IS NULL")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") c ON h.Hash = c.Hash");
            }

            if (replacer.JoinDetails)
            {
                sb
                    .AppendLine("LEFT JOIN HashDetails d ON h.Hash = d.Hash");
            }

            if (replacer.JoinTagCount)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, COUNT(*) TagCount")
                    .AppendLine("    FROM HashTag")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") tc ON h.Hash = tc.Hash");
            }

            if (replacer.JoinPersonCount)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, COUNT(*) PersonCount")
                    .AppendLine("    FROM HashPerson")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") pc ON h.Hash = pc.Hash");
            }

            if (replacer.JoinRatings)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, Value, Count, CASE WHEN NTile < 2 THEN 1 WHEN NTile < 4 THEN 2 WHEN NTile < 8 THEN 3 WHEN NTile < 10 THEN 4 ELSE 5 END AS Stars FROM (")
                    .AppendLine("        SELECT Hash, Value, Count, NTILE(10) OVER (PARTITION BY Category ORDER BY Value) NTile")
                    .AppendLine("        FROM Rating")
                    .AppendLine("        WHERE Category = ''")
                    .AppendLine("    ) z")
                    .AppendLine(") s ON h.Hash = s.Hash");
            }

            sb
                .AppendLine("WHERE (")
                .AppendLine(filter)
                .AppendLine(");");

            if (replacer.MaterializedTags.Count > 0)
            {
                sb.AppendLine("DROP TABLE temp.MaterializedTags;");
            }

            if (fetchTags)
            {
                sb.AppendLine("SELECT t.* FROM temp.SearchHashInfo h INNER JOIN HashTag t ON h.Hash = t.Hash;");
                sb.AppendLine("SELECT r.* FROM temp.SearchHashInfo h INNER JOIN RejectedTags r ON h.Hash = r.Hash;");
            }

            if (fetchPaths)
            {
                sb.AppendLine("SELECT p.* FROM temp.SearchHashInfo h INNER JOIN Paths p ON h.Hash = p.LastHash WHERE p.MissingSince IS NULL;");
            }

            if (fetchPeople)
            {
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.SearchHashPerson;")
                    .AppendLine("CREATE TEMP TABLE temp.SearchHashPerson (Hash text, PersonId int, PRIMARY KEY (Hash, PersonId));")
                    .AppendLine("INSERT INTO temp.SearchHashPerson (Hash, PersonId)")
                    .AppendLine("SELECT hp.* FROM temp.SearchHashInfo h INNER JOIN HashPerson hp ON h.Hash = hp.Hash;");
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.SearchRejectedPerson;")
                    .AppendLine("CREATE TEMP TABLE temp.SearchRejectedPerson (Hash text, PersonId int, PRIMARY KEY (Hash, PersonId));")
                    .AppendLine("INSERT INTO temp.SearchRejectedPerson (Hash, PersonId)")
                    .AppendLine("SELECT rp.* FROM temp.SearchHashInfo h INNER JOIN RejectedPerson rp ON h.Hash = rp.Hash;");

                if (fetchAliases)
                {
                    sb.AppendLine("SELECT PersonId, Site, Name FROM Alias WHERE PersonId IN (SELECT PersonId FROM temp.SearchHashPerson UNION SELECT PersonId FROM temp.SearchRejectedPerson);");
                }

                sb
                    .AppendLine("SELECT PersonId, Name FROM Person WHERE PersonId IN (SELECT PersonId FROM temp.SearchHashPerson UNION SELECT PersonId FROM temp.SearchRejectedPerson);");
                sb
                    .AppendLine("SELECT * FROM temp.SearchHashPerson;")
                    .AppendLine("DROP TABLE temp.SearchHashPerson;");
                sb
                    .AppendLine("SELECT * FROM temp.SearchRejectedPerson;")
                    .AppendLine("DROP TABLE temp.SearchRejectedPerson;");
            }

            if (fetchDetails)
            {
                sb.AppendLine("SELECT d.* FROM temp.SearchHashInfo h INNER JOIN HashDetails d ON h.Hash = d.Hash;");
            }

            if (fetchRatings)
            {
                sb
                    .AppendLine("SELECT r.Hash, r.Category, r.Value, r.Count")
                    .AppendLine("FROM temp.SearchHashInfo h")
                    .AppendLine("INNER JOIN Rating r")
                    .AppendLine("ON h.Hash = r.Hash")
                    .AppendLine("WHERE Category = '';");
            }

            if (fetchAny)
            {
                sb
                    .AppendLine("SELECT * FROM temp.SearchHashInfo;")
                    .AppendLine("DROP TABLE temp.SearchHashInfo;");
            }

            return sb.ToString();
        }

        private class SqlReplacer : ExpressionReplacer<string>
        {
            private string indent = string.Empty;

            public bool JoinCopies { get; private set; }

            public bool JoinDetails { get; private set; }

            public bool JoinPersonCount { get; private set; }

            public bool JoinRatings { get; private set; }

            public bool JoinTagCount { get; private set; }

            public ImmutableList<ImmutableHashSet<string>> MaterializedTags { get; private set; } = ImmutableList<ImmutableHashSet<string>>.Empty;

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

            public override string Replace(Expression expression)
            {
                var previousIndent = this.indent;
                try
                {
                    return base.Replace(expression);
                }
                finally
                {
                    this.indent = previousIndent;
                }
            }

            /// <inheritdoc/>
            public override string Replace(ConjunctionExpression expression)
            {
                var previousIndent = this.indent;
                this.indent = this.indent + "  ";

                var sb = new StringBuilder()
                    .Append(previousIndent)
                    .AppendLine("(");

                var first = true;
                foreach (var term in expression.Expressions)
                {
                    if (!first)
                    {
                        sb.Append(previousIndent).AppendLine(") AND (");
                    }

                    sb.Append(this.Replace(term));
                    first = false;
                }

                if (first)
                {
                    sb.Append(this.indent).AppendLine("1 = 1");
                }

                return sb.Append(previousIndent).AppendLine(")").ToString();
            }

            /// <inheritdoc/>
            public override string Replace(DisjunctionExpression expression)
            {
                var previousIndent = this.indent;
                this.indent = this.indent + "  ";

                var sb = new StringBuilder()
                    .Append(previousIndent)
                    .AppendLine("(");

                var first = true;
                foreach (var term in expression.Expressions)
                {
                    if (!first)
                    {
                        sb.Append(previousIndent).AppendLine(") OR (");
                    }

                    sb.Append(this.Replace(term));
                    first = false;
                }

                if (first)
                {
                    sb.Append(this.indent).AppendLine("1 = 0");
                }

                return sb.Append(previousIndent).AppendLine(")").ToString();
            }

            /// <inheritdoc/>
            public override string Replace(NegationExpression expression)
            {
                var previousIndent = this.indent;
                this.indent = this.indent + "  ";

                var sb = new StringBuilder()
                    .Append(previousIndent)
                    .AppendLine("NOT (");

                sb.Append(this.Replace(expression.Expression));

                return sb.Append(previousIndent).AppendLine(")").ToString();
            }

            public override string Replace(CopiesExpression expression)
            {
                this.JoinCopies = true;
                return $"{this.indent}COALESCE(c.Copies, 0) {ConvertOperator(expression.Operator)} {expression.Copies}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(DetailsExpression expression)
            {
                this.JoinDetails = true;
                return $"{this.indent}d.{EscapeName(expression.DetailsField)} IS NOT NULL AND d.{EscapeName(expression.DetailsField)} {ConvertOperator(expression.Operator)} {Literal(expression.Value)}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(FileSizeExpression expression) => $"{this.indent}FileSize {ConvertOperator(expression.Operator)} {expression.FileSize}{Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(HashExpression expression) => $"{this.indent}Hash {ConvertOperator(expression.Operator)} {Literal(expression.Value)}{Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(PeopleCountExpression expression)
            {
                this.JoinPersonCount = true;
                return $"{this.indent}COALESCE(pc.PersonCount, 0) {ConvertOperator(expression.Operator)} {expression.PeopleCount}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(PersonIdExpression expression) => $"{this.indent}EXISTS (SELECT 1 FROM HashPerson p WHERE h.Hash = p.Hash AND p.PersonId = {expression.PersonId}){Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(PersonNameExpression expression) => $"{this.indent}EXISTS (SELECT 1 FROM HashPerson hp INNER JOIN Names p ON hp.PersonId = p.PersonId WHERE h.Hash = hp.Hash AND {Contains("p.Name", expression.Value)}){Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(RatingExpression expression)
            {
                this.JoinRatings = true;
                return $"{this.indent}COALESCE(s.Value, {Storage.Rating.DefaultRating}) {ConvertOperator(expression.Operator)} {expression.Rating}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(RatingsCountExpression expression)
            {
                this.JoinRatings = true;
                return $"{this.indent}COALESCE(s.Count, 0) {ConvertOperator(expression.Operator)} {expression.RatingsCount}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(RejectedTagExpression expression) => $"{this.indent}EXISTS (SELECT 1 FROM RejectedTags t WHERE h.Hash = t.Hash AND t.Tag IN ({string.Join(", ", expression.Tags.Select(Literal))})){Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(StarsExpression expression)
            {
                this.JoinRatings = true;
                return $"{this.indent}COALESCE(s.Stars, 3) {ConvertOperator(expression.Operator)} {expression.Stars}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(TagExpression expression)
            {
                if (expression.Tags.Count > 3)
                {
                    var setIndex = -1;
                    for (var i = 0; i < this.MaterializedTags.Count; i++)
                    {
                        if (expression.Tags.SetEquals(this.MaterializedTags[i]))
                        {
                            setIndex = i;
                            break;
                        }
                    }

                    if (setIndex < 0)
                    {
                        setIndex = this.MaterializedTags.Count;
                        this.MaterializedTags = this.MaterializedTags.Add(expression.Tags);
                    }

                    return $"{this.indent}EXISTS (SELECT 1 FROM HashTag t WHERE h.Hash = t.Hash AND t.Tag IN (SELECT Tag FROM temp.MaterializedTags WHERE SetIndex = {setIndex})){Environment.NewLine}";
                }
                else
                {
                    return $"{this.indent}EXISTS (SELECT 1 FROM HashTag t WHERE h.Hash = t.Hash AND t.Tag IN ({string.Join(", ", expression.Tags.Select(Literal))})){Environment.NewLine}";
                }
            }

            /// <inheritdoc/>
            public override string Replace(TagCountExpression expression)
            {
                this.JoinTagCount = true;
                return $"{this.indent}COALESCE(tc.TagCount, 0) {ConvertOperator(expression.Operator)} {expression.TagCount}{Environment.NewLine}";
            }

            /// <inheritdoc/>
            public override string Replace(TextExpression expression) => $"{this.indent}EXISTS (SELECT 1 FROM Paths WHERE LastHash = h.Hash AND MissingSince IS NULL AND {Contains("Path", expression.Value)}){Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(TypeEqualsExpression expression) => $"{this.indent}FileType = {Literal(expression.Value)}{Environment.NewLine}";

            /// <inheritdoc/>
            public override string Replace(TypePrefixExpression expression) => $"{this.indent}{StartsWith("FileType", expression.Value)}{Environment.NewLine}";
        }
    }
}
