// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class NoPeopleExpression : Expression
    {
        public static readonly NoPeopleExpression Instance = new NoPeopleExpression();

        private NoPeopleExpression()
        {
        }
    }
}
