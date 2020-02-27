// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class DisjunctionTerm : Term
    {
        public static readonly int Precedence = 1;

        public DisjunctionTerm(IEnumerable<Term> terms)
        {
            this.Terms = terms.ToImmutableList();
        }

        public ImmutableList<Term> Terms { get; }

        /// <inheritdoc/>
        public override string ToString() => string.Join(" OR ", this.Terms.Select(t => GetPrecedence(t) < Precedence ? $"({t})" : t.ToString()));
    }
}
