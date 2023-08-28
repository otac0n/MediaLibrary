// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MediaLibrary.Properties;

    internal class AutoSearchManager
    {
        private long dirtyVersion;
        private long searchVersion;

        public event EventHandler<EventArgs> PerformSearch;

        public bool IsDirty => this.dirtyVersion != this.searchVersion;

        public async void Dirty()
        {
            var searchVersion = Interlocked.Increment(ref this.dirtyVersion);
            await Task.Delay(Settings.Default.AutoSearchDelay).ConfigureAwait(true);
            if (this.dirtyVersion != searchVersion)
            {
                return;
            }

            this.PerformSearchInternal();
        }

        public void Flush()
        {
            if (this.IsDirty)
            {
                this.PerformSearchInternal();
            }
        }

        public void Refresh() => this.PerformSearchInternal();

        private void PerformSearchInternal()
        {
            this.searchVersion = this.dirtyVersion;
            this.PerformSearch?.Invoke(this, EventArgs.Empty);
        }
    }
}
