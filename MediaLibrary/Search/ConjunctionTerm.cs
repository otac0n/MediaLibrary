// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class ConjunctionTerm : Term
    {
        public static readonly int Precedence = 0;

        public ConjunctionTerm(IEnumerable<Term> terms)
        {
            this.Terms = terms.ToImmutableList();
        }

        public ImmutableList<Term> Terms { get; }

        /// <inheritdoc/>
        public override string ToString() => string.Join(" ", this.Terms.Select(t => GetPrecedence(t) < Precedence ? $"({t})" : t.ToString()));
    }
}
