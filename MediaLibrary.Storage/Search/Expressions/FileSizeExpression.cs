// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class FileSizeExpression : Expression
    {
        public FileSizeExpression(string @operator, long fileSize)
        {
            this.Operator = @operator;
            this.FileSize = fileSize;
        }

        public long FileSize { get; }

        public string Operator { get; }
    }
}
