// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class TagCountExpression : Expression
    {
        public TagCountExpression(string @operator, int tagCount)
        {
            this.Operator = @operator;
            this.TagCount = tagCount;
        }

        public string Operator { get; }

        public int TagCount { get; }
    }
}
