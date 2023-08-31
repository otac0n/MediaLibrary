// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Terms
{
    public class NegationTerm : Term
    {
        public static readonly int Precedence = 2;

        public NegationTerm(Term negated)
        {
            this.Negated = negated;
        }

        public Term Negated { get; }

        /// <inheritdoc/>
        public override string ToString() =>
            GetPrecedence(this.Negated) <= Precedence ? $"-({this.Negated})" : $"-{this.Negated}";
    }
}
