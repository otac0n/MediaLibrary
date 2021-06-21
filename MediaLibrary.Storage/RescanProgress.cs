// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;

    public class RescanProgress
    {
        public RescanProgress(
            double estimate,
            int pathsDiscovered,
            int pathsProcessed,
            bool discoveryComplete)
        {
            this.Estimate = estimate;
            this.PathsDiscovered = pathsDiscovered;
            this.PathsProcessed = pathsProcessed;
            this.DiscoveryComplete = discoveryComplete;
        }

        public bool DiscoveryComplete { get; }

        public double Estimate { get; }

        public int PathsDiscovered { get; }

        public int PathsProcessed { get; }

        public static RescanProgress Aggregate(ref double lastProgress, params RescanProgress[] progresses)
        {
            var pathsDiscovered = 0;
            var pathsProcessed = 0;
            var discoveryComplete = true;
            for (var i = 0; i < progresses.Length; i++)
            {
                var p = progresses[i];
                pathsDiscovered += p.PathsDiscovered;
                pathsProcessed += p.PathsProcessed;
                discoveryComplete &= p.DiscoveryComplete;
            }

            var weight = discoveryComplete ? pathsDiscovered : Math.Max(pathsDiscovered + 100, pathsDiscovered / 0.99);
            var progress = weight == 0 ? 0 : pathsProcessed / weight;
            return new RescanProgress(
                lastProgress = Math.Max(lastProgress, progress),
                pathsDiscovered,
                pathsProcessed,
                discoveryComplete);
        }

        public RescanProgress Update(int? pathsDiscovered = null, int? pathsProcessed = null, bool? discoveryComplete = null)
        {
            var discovered = pathsDiscovered ?? this.PathsDiscovered;
            var processed = pathsProcessed ?? this.PathsProcessed;
            var complete = discoveryComplete ?? this.DiscoveryComplete;

            var weight = complete ? discovered : Math.Max(discovered + 100, discovered / 0.99);
            var progress = weight == 0 ? 0 : processed / weight;
            return new RescanProgress(
                Math.Max(this.Estimate, progress),
                discovered,
                processed,
                complete);
        }
    }
}
