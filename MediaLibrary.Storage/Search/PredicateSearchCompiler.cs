// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using MediaLibrary.Search;
    using MediaLibrary.Search.Sql;
    using MediaLibrary.Tagging;
    using static MediaLibrary.Search.Sql.QueryBuilder;

    public class PredicateSearchCompiler : PredicateCompiler<SearchResult>
    {
        private readonly bool excludeHidden;
        private readonly TagRuleEngine tagEngine;
        private int depth = 0;
        private PredicateDialect dialect;

        public PredicateSearchCompiler(TagRuleEngine tagEngine, bool excludeHidden)
        {
            this.tagEngine = tagEngine;
            this.excludeHidden = excludeHidden;
        }

        /// <inheritdoc/>
        public override Predicate<SearchResult> Compile(Term term)
        {
            var originalDepth = this.depth;
            this.depth++;
            try
            {
                if (originalDepth == 0)
                {
                    this.dialect = new PredicateDialect(this.tagEngine, this.excludeHidden, this);
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
        public override Predicate<SearchResult> CompileField(FieldTerm field)
        {
            return this.dialect.CompileField(field);
        }

        private Predicate<SearchResult> FinalizeQuery(Predicate<SearchResult> filter)
        {
            if (this.dialect.ExcludeHidden)
            {
                var oldFilter = filter;
                var hidden = this.dialect.Tag(this.tagEngine.GetTagDescendants("hidden").Add("hidden"));
                filter = x => oldFilter(x) && !hidden(x);
            }

            return filter;
        }

        private class PredicateDialect : SearchDialect<Predicate<SearchResult>>
        {
            public PredicateDialect(TagRuleEngine tagEngine, bool excludeHidden, PredicateSearchCompiler parentCompiler)
                : base(tagEngine, excludeHidden, parentCompiler)
            {
            }

            public override Predicate<SearchResult> Copies(string @operator, int value)
            {
                var op = ConvertOperator(@operator);
                return x => op(x.Paths.Count.CompareTo(value));
            }

            public override Predicate<SearchResult> Hash(string @operator, string value)
            {
                var op = ConvertOperator(@operator);
                return x => op(StringComparer.InvariantCultureIgnoreCase.Compare(x.Hash, value));
            }

            public override Predicate<SearchResult> NoPerson() => x => x.People.Count == 0;

            public override Predicate<SearchResult> PersonId(int value) => x => x.People.Any(p => p.PersonId == value);

            public override Predicate<SearchResult> PersonName(string value) => x => x.People.Any(p => p.Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1);

            public override Predicate<SearchResult> Rating(string @operator, double value)
            {
                var op = ConvertOperator(@operator);
                return x => op((x.Rating?.Value ?? Storage.Rating.DefaultRating).CompareTo(value));
            }

            public override Predicate<SearchResult> Stars(string @operator, int value)
            {
                var op = ConvertOperator(@operator);
                throw new NotImplementedException("Get stars statistics from the database, and compare to the percentiles.");
            }

            public override Predicate<SearchResult> Tag(ImmutableHashSet<string> value) => x => x.Tags.Any(value.Contains);

            public override Predicate<SearchResult> TagCount(string @operator, int value)
            {
                var op = ConvertOperator(@operator);
                return x => op(x.Tags.Count.CompareTo(value));
            }

            public override Predicate<SearchResult> TextSearch(string value) => x => x.Paths.Any(p => p.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > 0);

            public override Predicate<SearchResult> TypeEquals(string value) => x => x.FileType.Equals(value, StringComparison.OrdinalIgnoreCase);

            public override Predicate<SearchResult> TypePrefixed(string value) => x => x.FileType.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
