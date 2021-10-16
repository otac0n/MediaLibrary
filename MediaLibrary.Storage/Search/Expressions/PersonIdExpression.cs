// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class PersonIdExpression : Expression
    {
        public PersonIdExpression(int personId)
        {
            this.PersonId = personId;
        }

        public int PersonId { get; }
    }
}
