// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    internal abstract class PropertyParser<TSource>
    {
        protected PropertyParser(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public abstract bool TryGet(TSource source, out object value);
    }
}
