// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    using System;
    using System.Collections.Generic;

    internal class PropertyParserList<TSource> : List<PropertyParser<TSource>>
    {
        public void Add<TValue>(string name, PropertyGetter<TSource, TValue> getter)
        {
            this.Add(new PropertyParser<TSource, TValue>(name, getter));
        }

        public void Add<TValue>(string name, Func<TSource, TValue> getter)
        {
            this.Add(name, (TSource source, out TValue value) =>
            {
                value = getter(source);
                return true;
            });
        }

        public Dictionary<string, object> Recognize(TSource source)
        {
            var result = new Dictionary<string, object>();
            foreach (var item in this)
            {
                if (item.TryGet(source, out var value))
                {
                    result[item.Name] = value;
                }
            }

            return result;
        }
    }
}
