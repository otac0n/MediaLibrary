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

        public void Remove(TKey key)
        {
            lock (this.storage)
            {
                this.storage.Remove(key);
            }
        }

        public void RemoveAll(Func<TKey, TValue, bool> remove)
        {
            lock (this.storage)
            {
                var removeKeys = new HashSet<TKey>();
                foreach (var kvp in this.storage)
                {
                    if (!kvp.Value.TryGetTarget(out var value) ||
                        remove(kvp.Key, value))
                    {
                        removeKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in removeKeys)
                {
                    this.storage.Remove(key);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this.storage)
            {
                if (this.storage.TryGetValue(key, out var weakReference))
                {
                    if (weakReference.TryGetTarget(out value))
                    {
                        return true;
                    }
                    else
                    {
                        this.storage.Remove(key);
                    }
                }
                else
                {
                    value = default;
                }

                return false;
            }
        }

        public bool TryUpdate(TKey key, Action<TKey, TValue> updateValue)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException(nameof(updateValue));
            }

            lock (this.storage)
            {
                if (this.storage.TryGetValue(key, out var weakReference))
                {
                    if (weakReference.TryGetTarget(out var value))
                    {
                        updateValue(key, value);
                        return true;
                    }
                    else
                    {
                        this.storage.Remove(key);
                    }
                }
            }

            return false;
        }

        public void UpdateAll(Action<TKey, TValue> update)
        {
            lock (this.storage)
            {
                var removeKeys = new HashSet<TKey>();
                foreach (var kvp in this.storage)
                {
                    if (kvp.Value.TryGetTarget(out var value))
                    {
                        update(kvp.Key, value);
                    }
                    else
                    {
                        removeKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in removeKeys)
                {
                    this.storage.Remove(key);
                }
            }
        }
    }
}
