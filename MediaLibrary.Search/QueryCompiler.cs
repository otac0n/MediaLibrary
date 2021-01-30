// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class QueryCompiler<TQuery>
    {
        private Func<string, Term> getSavedSearch;

        protected QueryCompiler(Func<string, Term> getSavedSearch)
        {
            this.getSavedSearch = getSavedSearch;
        }

        public virtual TQuery Compile(Term term)
        {
            switch (term)
            {
                case ConjunctionTerm conjunction:
                    return this.Compile(conjunction);

                case DisjunctionTerm disjunction:
                    return this.Compile(disjunction);

                case NegationTerm negation:
                    return this.Compile(negation);

                case FieldTerm field:
                    return this.CompileField(field);

                case SavedSearchTerm savedSearch:
                    return this.CompileSavedSearch(savedSearch);

                default:
                    throw new NotSupportedException();
            }
        }

        public virtual TQuery Compile(ConjunctionTerm conjunction) =>
            this.CompileConjunction(
                (conjunction ?? throw new ArgumentNullException(nameof(conjunction)))
                .Terms.Select(t => this.Compile(t)));

        public virtual TQuery Compile(DisjunctionTerm disjunction) =>
            this.CompileDisjunction(
                (disjunction ?? throw new ArgumentNullException(nameof(disjunction)))
                .Terms.Select(t => this.Compile(t)));

        public virtual TQuery Compile(NegationTerm negation) =>
            this.CompileNegation(this.Compile((negation ?? throw new ArgumentNullException(nameof(negation))).Negated));

        public abstract TQuery CompileConjunction(IEnumerable<TQuery> query);

        public abstract TQuery CompileDisjunction(IEnumerable<TQuery> query);

        public abstract TQuery CompileField(FieldTerm field);

        public abstract TQuery CompileNegation(TQuery query);

        public virtual TQuery CompileSavedSearch(SavedSearchTerm savedSearch)
        {
            // TODO: Handle recursion, avoid stack overflow.
            var term = this.getSavedSearch(savedSearch.SearchName);
            return term == null
                ? this.CompileDisjunction(Array.Empty<TQuery>())
                : this.Compile(term);
        }
    }
}
