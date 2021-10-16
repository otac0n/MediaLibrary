// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class PeopleCountExpression : Expression
    {
        public PeopleCountExpression(string @operator, int peopleCount)
        {
            this.Operator = @operator;
            this.PeopleCount = peopleCount;
        }

        public string Operator { get; }

        public int PeopleCount { get; }
    }
}
