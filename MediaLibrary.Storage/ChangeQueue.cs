// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class ChangeQueue<TKey, TValue>
    {
        private static readonly Comparer<(TimeSpan, TKey, TValue)> ItemComparer = Comparer<(TimeSpan due, TKey, TValue)>.Create((a, b) => a.due.CompareTo(b.due));
        private readonly Stopwatch epoch = Stopwatch.StartNew();
        private readonly Func<TValue, TKey> getKey;
        private readonly HashSet<TKey> keys = new HashSet<TKey>();
        private readonly List<(TimeSpan due, TKey key, TValue item)> queue = new List<(TimeSpan, TKey, TValue)>();

        public ChangeQueue(Func<TValue, TKey> getKey)
        {
            this.getKey = getKey;
        }

        public int Count
        {
            get
            {
                lock (this.queue)
                {
                    return this.queue.Count;
                }
            }
        }

        public TimeSpan? CurrentDelay
        {
            get
            {
                TimeSpan due;
                lock (this.queue)
                {
                    if (this.queue.Count == 0)
                    {
                        return null;
                    }

                    due = this.queue[0].due;
                }

                var delay = due - this.epoch.Elapsed;
                return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
            }
        }

        public bool Dequeue(out TValue item)
        {
            lock (this.queue)
            {
                if (this.queue.Count > 0)
                {
                    var now = this.epoch.Elapsed;
                    var (due, key, value) = this.queue[0];
                    if (due <= now)
                    {
                        this.queue.RemoveAt(0);
                        this.keys.Remove(key);
                        item = value;
                        return true;
                    }
                }
            }

            item = default;
            return false;
        }

        public void Enqueue(TValue item, TimeSpan delay = default)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            var key = this.getKey(item);
            var due = this.epoch.Elapsed + delay;
            lock (this.queue)
            {
                if (this.keys.Add(key))
                {
                    var value = (due, key, item);
                    var index = this.queue.BinarySearch(value, ItemComparer);
                    this.queue.Insert(index < 0 ? ~index : index, value);
                }
            }
        }
    }
}
