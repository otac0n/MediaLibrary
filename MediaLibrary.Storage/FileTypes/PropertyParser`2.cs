// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    using System;

    internal class PropertyParser<TSource, TValue> : PropertyParser<TSource>
    {
        public PropertyParser(string name, PropertyGetter<TSource, TValue> getter)
            : base(name)
        {
            this.Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        }

        public PropertyGetter<TSource, TValue> Getter { get; }

        public override bool TryGet(TSource source, out object value)
        {
            var result = this.Getter(source, out var valueOfT);
            value = valueOfT;
            return result;
        }
    }
}
