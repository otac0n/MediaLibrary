// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using MediaLibrary.Tagging;
    using static QueryBuilder;

    public class SearchDialect : AnsiSqlCompiler
    {
        private readonly TagRuleEngine tagEngine;
        private int depth = 0;
        private bool excludeHidden;
        private bool joinCopies = false;
        private bool joinTagCount = false;

        public SearchDialect(TagRuleEngine tagEngine, bool excludeHidden = true)
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
                    this.joinCopies = false;
                    this.joinTagCount = false;
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
                case null:
                    return $"EXISTS (SELECT 1 FROM Paths WHERE LastHash = h.Hash AND MissingSince IS NULL AND {Contains("Path", field.Value)})";

                case "@":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    if (int.TryParse(field.Value, out var personId))
                    {
                        if (personId == 0)
                        {
                            return $"NOT EXISTS (SELECT 1 FROM HashPerson p WHERE h.Hash = p.Hash)";
                        }
                        else
                        {
                            return $"EXISTS (SELECT 1 FROM HashPerson p WHERE h.Hash = p.Hash AND p.PersonId = {personId})";
                        }
                    }
                    else
                    {
                        return $"EXISTS (SELECT 1 FROM HashPerson hp INNER JOIN Names p ON hp.PersonId = p.PersonId WHERE h.Hash = hp.Hash AND {Contains("p.Name", field.Value)})";
                    }

                case "type":
                    if (field.Operator != FieldTerm.EqualsOperator)
                    {
                        throw new NotSupportedException($"Cannot use operator '{field.Operator}' with field '{field.Field}'.");
                    }

                    var ix = field.Value.IndexOf('/');
                    return
                        ix < 0 ? $"FileType = {Literal(field.Value)} OR {StartsWith("FileType", field.Value + "/")}" :
                        ix == field.Value.Length - 1 ? StartsWith("FileType", field.Value) :
                        $"FileType = {Literal(field.Value)}";

                case "tag":
                    var tagInfo = this.tagEngine[field.Value];
                    if (this.excludeHidden && (tagInfo.Tag == "hidden" || tagInfo.Ancestors.Contains("hidden")))
                    {
                        this.excludeHidden = false;
                    }

                    var tags = ImmutableHashSet<string>.Empty;
                    switch (field.Operator)
                    {
                        case FieldTerm.GreaterThanOperator:
                        case FieldTerm.GreaterThanOrEqualOperator:

                            tags = tags.Union(tagInfo.Ancestors);

                            if (field.Operator == FieldTerm.GreaterThanOrEqualOperator)
                            {
                                goto case FieldTerm.EqualsOperator;
                            }

                            break;

                        case FieldTerm.LessThanOperator:
                        case FieldTerm.LessThanOrEqualOperator:

                            tags = tags.Union(tagInfo.Descendants);

                            if (field.Operator == FieldTerm.LessThanOrEqualOperator)
                            {
                                goto case FieldTerm.EqualsOperator;
                            }

                            break;

                        case FieldTerm.EqualsOperator:
                            tags = tags.Add(tagInfo.Tag);
                            break;
                    }

                    tags = tags.Union(tags.SelectMany(this.tagEngine.GetTagAliases));
                    return $"EXISTS (SELECT 1 FROM HashTag t WHERE h.Hash = t.Hash AND t.Tag IN ({string.Join(", ", tags.Select(Literal))}))";

                case "copies":
                    this.joinCopies = true;
                    if (!int.TryParse(field.Value, out var copies))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return $"COALESCE(c.Copies, 0) {ConvertOperator(field.Operator)} {copies}";

                case "tags":
                    this.joinTagCount = true;
                    if (!int.TryParse(field.Value, out var tagCount))
                    {
                        throw new NotSupportedException($"Cannot use non-numeric value '{field.Value}' with field '{field.Field}'.");
                    }

                    return $"COALESCE(tc.TagCount, 0) {ConvertOperator(field.Operator)} {tagCount}";

                case "hash":
                    return $"Hash {ConvertOperator(field.Operator)} {Literal(field.Value)}";

                default:
                    throw new NotSupportedException();
            }
        }

        private static string Contains(string expr, string patternValue)
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

        private static string ConvertOperator(string fieldOperator)
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

        private static string StartsWith(string expr, string patternExpr, char? escape) => Like(expr, patternExpr, escape);

        private static string StartsWith(string expr, string patternValue)
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

        private string FinalizeQuery(string filter)
        {
            var fetchTags = true;
            var fetchPaths = true;
            var fetchPeople = true;
            var fetchAliases = true && fetchPeople;
            var fetchAny = fetchTags || fetchPaths || fetchPeople;

            var sb = new StringBuilder();

            if (fetchAny)
            {
                sb
                    .AppendLine("DROP TABLE IF EXISTS temp.SearchHashInfo;")
                    .AppendLine("CREATE TEMP TABLE temp.SearchHashInfo (Hash text, FileSize integer, FileType text, PRIMARY KEY (Hash));")
                    .AppendLine("INSERT INTO temp.SearchHashInfo (Hash, FileSize, FileType)");
            }

            sb
                .AppendLine("SELECT h.Hash, h.FileSize, h.FileType")
                .AppendLine("FROM HashInfo h");

            if (this.joinCopies)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT LastHash Hash, COUNT(*) Copies")
                    .AppendLine("    FROM Paths")
                    .AppendLine("    WHERE MissingSince IS NULL")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") c ON h.Hash = c.Hash");
            }

            if (this.joinTagCount)
            {
                sb
                    .AppendLine("LEFT JOIN (")
                    .AppendLine("    SELECT Hash, COUNT(*) TagCount")
                    .AppendLine("    FROM HashTag")
                    .AppendLine("    GROUP BY Hash")
                    .AppendLine(") tc ON h.Hash = tc.Hash");
            }

            sb
                .AppendLine("WHERE (")
                .AppendLine(filter);
            if (this.excludeHidden)
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

                if (fetchAliases)
                {
                    sb.AppendLine("SELECT PersonId, Site, Name FROM Alias WHERE PersonId IN (SELECT PersonId FROM temp.SearchHashPerson);");
                }

                sb
                    .AppendLine("SELECT PersonId, Name FROM Person WHERE PersonId IN (SELECT PersonId FROM temp.SearchHashPerson);")
                    .AppendLine("SELECT * FROM temp.SearchHashPerson;")
                    .AppendLine("DROP TABLE temp.SearchHashPerson;");
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
