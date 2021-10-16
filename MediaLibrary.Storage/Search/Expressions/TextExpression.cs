// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public class TextExpression : Expression
    {
        public TextExpression(string value)
        {
            this.Value = value;
        }

        public string Value { get; }
    }
}
