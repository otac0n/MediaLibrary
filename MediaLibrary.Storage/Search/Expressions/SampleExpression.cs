// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    public sealed class SampleExpression : Expression
    {
        public SampleExpression(double portion)
        {
            this.Portion = portion;
        }

        public double Portion { get; }
    }
}
