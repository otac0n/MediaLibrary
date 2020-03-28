// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Generic;

    public class WeakReferenceCache<TKey, TValue>
        where TValue : class
    {
        private readonly Dictionary<TKey, WeakReference<TValue>> storage;

        public WeakReferenceCache()
        {
            this.storage = new Dictionary<TKey, WeakReference<TValue>>();
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> createValue, Action<TKey, TValue> updateValue)
        {
            if (createValue == null)
            {
                throw new ArgumentNullException(nameof(createValue));
            }

            if (updateValue == null)
            {
                throw new ArgumentNullException(nameof(updateValue));
            }

            lock (this.storage)
            {
                if (!this.storage.TryGetValue(key, out var weakReference) ||
                    !weakReference.TryGetTarget(out var value))
                {
                    this.storage[key] = new WeakReference<TValue>(value = createValue(key));
                }
                else
                {
                    updateValue(key, value);
                }

                return value;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> createValue)
        {
            if (createValue == null)
            {
                throw new ArgumentNullException(nameof(createValue));
            }

            lock (this.storage)
            {
                if (!this.storage.TryGetValue(key, out var weakReference) ||
                    !weakReference.TryGetTarget(out var value))
                {
                    this.storage[key] = new WeakReference<TValue>(value = createValue(key));
                }

                return value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this.storage)
            {
                if (!this.storage.TryGetValue(key, out var weakReference))
                {
                    value = default;
                    return false;
                }

                if (!weakReference.TryGetTarget(out value))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
