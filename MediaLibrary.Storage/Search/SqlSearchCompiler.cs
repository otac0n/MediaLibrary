// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using MediaLibrary.Search;
    using MediaLibrary.Search.Sql;
    using TaggingLibrary;
    using static MediaLibrary.Search.Sql.QueryBuilder;

    public class SqlSearchCompiler : AnsiSqlCompiler
    {
        private readonly bool excludeHidden;
        private readonly TagRuleEngine tagEngine;
        private int depth = 0;
        private SqlDialect dialect;

        public SqlSearchCompiler(TagRuleEngine tagEngine, bool excludeHidden, Func<string, Term> getSavedSearch)
            : base(getSavedSearch)
        {
            this.tagEngine = tagEngine;
            this.excludeHidden = excludeHidden;
        }

        /// <inheritdoc/>
        public override string Compile(Term term)
        {
            var originalDepth = this.depth;
            this.depth++;
            try
            {
                if (originalDepth == 0)
                {
                    this.dialect = new SqlDialect(this.tagEngine, this.excludeHidden, this);
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
            return this.dialect.CompileField(field);
        }

        private string FinalizeQuery(string filter)
        {
            var fetchTags = true;
            var fetchPaths = true;
            var fetchPeople = true;
            var fetchAliases = true && fetchPeople;
            var fetchRatings = true;
            var fetchAny = fetchTags || fetchPaths || fetchPeople || fetchRatings;

            var sb = new StringBuilder();

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

            if (this.dialect.JoinCopies)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT LastHash Hash, COUNT(*) Copies")
                    .AppendLine("    FROM Paths")
                    .AppendLine("    WHERE MissingSince IS NULL")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") c ON h.Hash = c.Hash");
            }

            if (this.dialect.JoinDetails)
            {
                sb
                    .AppendLine("LEFT JOIN HashDetails d ON h.Hash = d.Hash");
            }

            if (this.dialect.JoinTagCount)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, COUNT(*) TagCount")
                    .AppendLine("    FROM HashTag")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") tc ON h.Hash = tc.Hash");
            }

            if (this.dialect.JoinPersonCount)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, COUNT(*) PersonCount")
                    .AppendLine("    FROM HashPerson")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") pc ON h.Hash = pc.Hash");
            }

            if (this.dialect.JoinStars)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, CASE WHEN NTile < 2 THEN 1 WHEN NTile < 4 THEN 2 WHEN NTile < 8 THEN 3 WHEN NTile < 10 THEN 4 ELSE 5 END AS Stars FROM (")
                    .AppendLine("        SELECT Hash, NTILE(10) OVER (PARTITION BY Category ORDER BY Value) NTile")
                    .AppendLine("        FROM Rating")
                    .AppendLine("        WHERE Category = ''")
                    .AppendLine("    ) z")
                    .AppendLine(") s ON h.Hash = s.Hash");
            }

            sb
                .AppendLine("WHERE (")
                .AppendLine(filter);
            if (this.dialect.ExcludeHidden)
            {
                var tags = this.tagEngine.GetTagDescendants("hidden").Add("hidden");
                sb
                    .AppendLine(")")
                    .Append("AND NOT EXISTS (SELECT 1 FROM HashTag t WHERE h.Hash = t.Hash AND t.Tag IN (")
                    .Append(string.Join(", ", tags.Select(Literal)))
                    .Append(")");
            }

            sb.AppendLine(");");

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

        private class SqlDialect : SearchDialect<string>
        {
            public SqlDialect(TagRuleEngine tagEngine, bool excludeHidden, AnsiSqlCompiler parentCompiler)
                : base(tagEngine, excludeHidden, parentCompiler)
            {
            }

            public bool JoinCopies { get; private set; }

            public bool JoinDetails { get; private set; }

            public bool JoinPersonCount { get; private set; }

            public bool JoinStars { get; private set; }

            public bool JoinTagCount { get; private set; }

            public override string Copies(string @operator, int value)
            {
                this.JoinCopies = true;
                return $"COALESCE(c.Copies, 0) {ConvertOperator(@operator)} {value}";
            }

            public override string Details(string detailsField, string @operator, object value)
            {
                this.JoinDetails = true;
                return $"d.{EscapeName(detailsField)} {ConvertOperator(@operator)} {Literal(value)}";
            }

            public override string FileSize(string @operator, long value) => $"FileSize {ConvertOperator(@operator)} {value}";

            public override string Hash(string @operator, string value) => $"Hash {ConvertOperator(@operator)} {Literal(value)}";

            public override string PersonCount(string @operator, int value)
            {
                this.JoinPersonCount = true;
                return $"COALESCE(pc.PersonCount, 0) {ConvertOperator(@operator)} {value}";
            }

            public override string PersonId(int value) => $"EXISTS (SELECT 1 FROM HashPerson p WHERE h.Hash = p.Hash AND p.PersonId = {value})";

            public override string PersonName(string value) => $"EXISTS (SELECT 1 FROM HashPerson hp INNER JOIN Names p ON hp.PersonId = p.PersonId WHERE h.Hash = hp.Hash AND {Contains("p.Name", value)})";

            public override string Rating(string @operator, double value)
            {
                this.JoinStars = true;
                return $"COALESCE(s.Rating, {Storage.Rating.DefaultRating}) {ConvertOperator(@operator)} {value}";
            }

            public override string RejectedTag(ImmutableHashSet<string> value) => $"EXISTS (SELECT 1 FROM RejectedTags t WHERE h.Hash = t.Hash AND t.Tag IN ({string.Join(", ", value.Select(Literal))}))";

            public override string Stars(string @operator, int value)
            {
                this.JoinStars = true;
                return $"COALESCE(s.Stars, 3) {ConvertOperator(@operator)} {value}";
            }

            public override string Tag(ImmutableHashSet<string> value) => $"EXISTS (SELECT 1 FROM HashTag t WHERE h.Hash = t.Hash AND t.Tag IN ({string.Join(", ", value.Select(Literal))}))";

            public override string TagCount(string @operator, int value)
            {
                this.JoinTagCount = true;
                return $"COALESCE(tc.TagCount, 0) {ConvertOperator(@operator)} {value}";
            }

            public override string TextSearch(string value) => $"EXISTS (SELECT 1 FROM Paths WHERE LastHash = h.Hash AND MissingSince IS NULL AND {Contains("Path", value)})";

            public override string TypeEquals(string value) => $"FileType = {Literal(value)}";

            public override string TypePrefixed(string value) => StartsWith("FileType", value);
        }
    }
}
