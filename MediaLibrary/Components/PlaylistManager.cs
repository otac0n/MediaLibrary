// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class PlaylistManager
    {
        public static PlaylistManager<T> Create<T>(IEnumerable<T> items, bool shuffle = false, bool repeat = false) =>
            new PlaylistManager<T>(items)
            {
                Repeat = repeat,
                Shuffle = shuffle,
            };
    }

    public class PlaylistManager<T>
    {
        private readonly List<int> history = new List<int>();
        private readonly IReadOnlyList<T> originalOrder;
        private readonly Random random = new Random();
        private int historyIndex;

        public PlaylistManager(IEnumerable<T> items)
        {
            this.originalOrder = items.ToList().AsReadOnly();
        }

        public int Count => this.originalOrder.Count;

        public T Current
        {
            get
            {
                if (this.historyIndex >= this.history.Count ||
                    this.history[this.historyIndex] >= this.Count)
                {
                    return default;
                }

                return this.originalOrder[this.history[this.historyIndex]];
            }
        }

        public bool Repeat { get; set; }

        public bool Shuffle { get; set; }

        public bool Next()
        {
            if (this.Count == 0)
            {
                // If there are no items in the playlist, we cannot move forward.
                return false;
            }

            int nextIndex;
            if (this.Shuffle)
            {
                if (this.history.Count == 0)
                {
                    // Randomly choose the first item to select.
                    this.history.Add(nextIndex = this.random.Next(this.Count));
                    return true;
                }
                else
                {
                    // TODO: Support samples that don't repeat as frequently.
                    // TODO: Don't advance past one playlist.
                    nextIndex = this.random.Next(this.Count);
                }
            }
            else
            {
                if (this.history.Count == 0)
                {
                    // In linear mode, the first item is always the first item in the original order.
                    this.history.Add(nextIndex = 0);
                    return true;
                }
                else
                {
                    // In linear order, increase the index by one
                    nextIndex = this.history[this.historyIndex] + 1;
                    if (nextIndex >= this.Count)
                    {
                        if (this.Repeat)
                        {
                            nextIndex = nextIndex % this.Count;
                        }
                        else
                        {
                            // In linear order, we cannot advance past the end of the list.
                            return false;
                        }
                    }
                }
            }

            if (this.historyIndex < this.history.Count - 1 &&
                this.history[this.historyIndex + 1] == nextIndex)
            {
                // We can reuse the history, because the next index will have the appropriate value.
                this.historyIndex++;
            }
            else
            {
                // TODO: Clear any shuffle history.
                this.historyIndex++;
                this.history.RemoveRange(this.historyIndex, this.history.Count - this.historyIndex);
                this.history.Add(nextIndex);
            }

            return true;
        }

        public bool Previous()
        {
            if (this.Count == 0)
            {
                return false;
            }

            if (this.historyIndex > 0)
            {
                this.historyIndex--;
                return true;
            }
            else if (this.Repeat && !this.Shuffle)
            {
                this.history.Insert(this.historyIndex, (this.history[this.historyIndex] - 1 + this.Count) % this.Count);
                return true;
            }

            return false;
        }
    }
}
