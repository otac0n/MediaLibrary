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
            var weight = 0.0;
            var pathsDiscovered = 0;
            var pathsProcessed = 0;
            var discoveryComplete = true;
            for (var i = 0; i < progresses.Length; i++)
            {
                var p = progresses[i];
                pathsDiscovered += p.PathsDiscovered;
                pathsProcessed += p.PathsProcessed;
                discoveryComplete &= p.DiscoveryComplete;
                weight += p.DiscoveryComplete ? p.PathsDiscovered : Math.Max(p.PathsDiscovered + 100, p.PathsDiscovered * 2);
            }

            var progress = weight == 0 ? 0 : pathsProcessed / weight;
            return new RescanProgress(
                lastProgress = Math.Max(lastProgress, progress * (discoveryComplete ? 1 : 0.99)),
                pathsDiscovered,
                pathsProcessed,
                discoveryComplete);
        }
    }
}
