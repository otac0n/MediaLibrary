// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    using System.Collections.Immutable;

    public sealed class RejectedTagExpression : Expression
    {
        public RejectedTagExpression(ImmutableHashSet<string> tags)
        {
            this.Tags = tags;
        }

        public ImmutableHashSet<string> Tags
        {
            get;
        }
    }
}
