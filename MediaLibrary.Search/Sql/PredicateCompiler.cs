// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class PredicateCompiler<T> : QueryCompiler<Predicate<T>>
    {
        public PredicateCompiler(Func<string, Term> getSavedSearch)
            : base(getSavedSearch)
        {
        }

        public static Func<int, bool> ConvertOperator(string fieldOperator)
        {
            switch (fieldOperator)
            {
                case FieldTerm.EqualsOperator:
                    return x => x == 0;

                case FieldTerm.GreaterThanOperator:
                    return x => x > 0;

                case FieldTerm.GreaterThanOrEqualOperator:
                    return x => x >= 0;

                case FieldTerm.LessThanOperator:
                    return x => x < 0;

                case FieldTerm.LessThanOrEqualOperator:
                    return x => x <= 0;

                default:
                    throw new NotSupportedException($"Unrecognized operator '{fieldOperator}'.");
            }
        }

        /// <inheritdoc/>
        public override Predicate<T> CompileConjunction(IEnumerable<Predicate<T>> query)
        {
            var predicates = query.ToList();
            return t => predicates.All(p => p(t));
        }

        /// <inheritdoc/>
        public override Predicate<T> CompileDisjunction(IEnumerable<Predicate<T>> query)
        {
            var predicates = query.ToList();
            return t => predicates.Any(p => p(t));
        }

        /// <inheritdoc/>
        public override Predicate<T> CompileNegation(Predicate<T> query)
        {
            return x => !query(x);
        }
    }
}
