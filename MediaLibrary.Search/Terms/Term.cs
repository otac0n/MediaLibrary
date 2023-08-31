// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Terms
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class Term
    {
        public static int GetPrecedence(Term term)
        {
            switch (term)
            {
                case FieldTerm _: return FieldTerm.Precedence;
                case NegationTerm _: return NegationTerm.Precedence;
                case DisjunctionTerm _: return DisjunctionTerm.Precedence;
                case ConjunctionTerm _: return ConjunctionTerm.Precedence;
                default: throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public abstract override string ToString();
    }
}
