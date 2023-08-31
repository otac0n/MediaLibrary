// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Terms
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class PropertyConjunctionTerm : Term
    {
        public PropertyConjunctionTerm(IEnumerable<PropertyPredicate> predicates)
        {
            this.Predicates = predicates.ToImmutableList();
        }

        public ImmutableList<PropertyPredicate> Predicates { get; }

        /// <inheritdoc/>
        public override string ToString() => $"[{string.Join(" ", this.Predicates)}]";
    }
}
