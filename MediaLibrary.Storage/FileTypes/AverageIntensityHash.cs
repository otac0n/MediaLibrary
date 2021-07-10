// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Linq;
    using System.Runtime.InteropServices;

    public class AverageIntensityHash
    {
        /// <summary>
        /// Computes the minimum difference between flips and rotations of two hashes.
        /// </summary>
        /// <param name="a">The first hash to compare.</param>
        /// <param name="b">The second hash to compare.</param>
        /// <param name="mode">The flip/rotate mode. <c>0</c>, to not flip or rotate, <c>1</c> to flip horizontally, <c>2</c> to flip and rotate in all directions.</param>
        /// <returns>The minimum distance between the two hashes, considering flips and rotations.</returns>
        public static int Distance(ulong a, ulong b, int mode = 0)
        {
            var expandedA = Expand(a, mode);
            return (from ea in expandedA
                    select CountBits(ea ^ b)).Min();
        }

        /// <summary>
        /// Gets the rotation and flip variations of the specified <paramref name="hash"/>.
        /// </summary>
        /// <param name="hash">The hash to rotate and flip.</param>
        /// <param name="mode">The flip/rotate mode. <c>0</c>, to not flip or rotate, <c>1</c> to flip horizontally, <c>2</c> to flip and rotate in all directions.</param>
        /// <returns>The distinct variations of the specified <paramref name="hash"/>.</returns>
        public static HashSet<ulong> Expand(ulong hash, int mode = 0)
        {
            var samples = new HashSet<ulong>
            {
                hash,
            };

            if (mode > 0)
            {
                var hashLR = FlipLeftRight(hash);
                samples.Add(hashLR);
                if (mode > 1)
                {
                    samples.Add(FlipTopBottom(hash));
                    samples.Add(FlipTopBottom(hashLR));
                    samples.UnionWith(samples.Select(RotateQuarterTurnClockwise).ToList());
                }
            }

            return samples;
        }

        /// <summary>
        /// Gets the hash of the specified <paramref name="image"/> in its current orientation.
        /// </summary>
        /// <param name="image">The <see cref="Image"/> to hash.</param>
        /// <returns>The hash of the image based on the average intensity scaled over the <paramref name="image"/>.</returns>
        public static ulong GetImageHash(Image image)
        {
            const int HashWidth = 8;
            const int HashHeight = 8;
            var gamma = ImageDetailRecognizer.GetGamma(image);

            using (var squash = new Bitmap(HashWidth, HashHeight))
            {
                var bounds = new Rectangle(0, 0, HashWidth, HashHeight);
                using (var g = Graphics.FromImage(squash))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(image, bounds);
                }

                var data = squash.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                double Value(int value)
                {
                    var b = (value >> (8 * 0)) & 0xFF;
                    var g = (value >> (8 * 1)) & 0xFF;
                    var r = (value >> (8 * 2)) & 0xFF;
                    return Math.Pow(b, gamma) + Math.Pow(g, gamma) + Math.Pow(r, gamma);
                }

                var total = 0.0;
                var row = new int[HashWidth];
                for (var y = 0; y < HashHeight; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, HashWidth);
                    for (var x = 0; x < HashWidth; x++)
                    {
                        total += Value(row[x]);
                    }
                }

                var target = total / (HashWidth * HashHeight);

                var hash = 0UL;
                for (var y = 0; y < HashHeight; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, HashWidth);
                    for (var x = 0; x < HashWidth; x++)
                    {
                        hash = (hash << 1) | (Value(row[x]) >= target ? 1UL : 0UL);
                    }
                }

                squash.UnlockBits(data);
                return hash;
            }
        }

        /// <remarks>
        /// Hamming weight algorithm, adapted from https://en.wikipedia.org/wiki/Hamming_weight.
        /// </remarks>
        private static int CountBits(ulong difference)
        {
            unchecked
            {
                var byPairs = difference - ((difference >> 1) & 0x5555555555555555);
                var byQuads = (byPairs & 0x3333333333333333) + ((byPairs >> 2) & 0x3333333333333333);
                var byOctets = (byQuads + (byQuads >> 4)) & 0x0f0f0f0f0f0f0f0f;
                return (int)((byOctets * 0x0101010101010101UL) >> 56);
            }
        }

        private static ulong FlipLeftRight(ulong hash) =>
            ((hash & 0x8080808080808080) >> 7) |
            ((hash & 0x4040404040404040) >> 5) |
            ((hash & 0x2020202020202020) >> 3) |
            ((hash & 0x1010101010101010) >> 1) |
            ((hash & 0x0808080808080808) << 1) |
            ((hash & 0x0404040404040404) << 3) |
            ((hash & 0x0202020202020202) << 5) |
            ((hash & 0x0101010101010101) << 7);

        private static ulong FlipTopBottom(ulong hash) =>
            ((hash & 0xff00000000000000) >> 56) |
            ((hash & 0x00ff000000000000) >> 40) |
            ((hash & 0x0000ff0000000000) >> 24) |
            ((hash & 0x000000ff00000000) >> 8) |
            ((hash & 0x00000000ff000000) << 8) |
            ((hash & 0x0000000000ff0000) << 24) |
            ((hash & 0x000000000000ff00) << 40) |
            ((hash & 0x00000000000000ff) << 56);

        private static ulong RotateQuarterTurnClockwise(ulong hash) =>
            ((hash & 0x8000000000000000) >> 56) |
            ((hash & 0x4000000000000000) >> 47) |
            ((hash & 0x2000000000000000) >> 38) |
            ((hash & 0x1000000000000000) >> 29) |
            ((hash & 0x0800000000000000) >> 20) |
            ((hash & 0x0400000000000000) >> 11) |
            ((hash & 0x0200000000000000) >> 2) |
            ((hash & 0x0100000000000000) << 7) |
            ((hash & 0x0080000000000000) >> 49) |
            ((hash & 0x0040000000000000) >> 40) |
            ((hash & 0x0020000000000000) >> 31) |
            ((hash & 0x0010000000000000) >> 22) |
            ((hash & 0x0008000000000000) >> 13) |
            ((hash & 0x0004000000000000) >> 4) |
            ((hash & 0x0002000000000000) << 5) |
            ((hash & 0x0001000000000000) << 14) |
            ((hash & 0x0000800000000000) >> 42) |
            ((hash & 0x0000400000000000) >> 33) |
            ((hash & 0x0000200000000000) >> 24) |
            ((hash & 0x0000100000000000) >> 15) |
            ((hash & 0x0000080000000000) >> 6) |
            ((hash & 0x0000040000000000) << 3) |
            ((hash & 0x0000020000000000) << 12) |
            ((hash & 0x0000010000000000) << 21) |
            ((hash & 0x0000008000000000) >> 35) |
            ((hash & 0x0000004000000000) >> 26) |
            ((hash & 0x0000002000000000) >> 17) |
            ((hash & 0x0000001000000000) >> 8) |
            ((hash & 0x0000000800000000) << 1) |
            ((hash & 0x0000000400000000) << 10) |
            ((hash & 0x0000000200000000) << 19) |
            ((hash & 0x0000000100000000) << 28) |
            ((hash & 0x0000000080000000) >> 28) |
            ((hash & 0x0000000040000000) >> 19) |
            ((hash & 0x0000000020000000) >> 10) |
            ((hash & 0x0000000010000000) >> 1) |
            ((hash & 0x0000000008000000) << 8) |
            ((hash & 0x0000000004000000) << 17) |
            ((hash & 0x0000000002000000) << 26) |
            ((hash & 0x0000000001000000) << 35) |
            ((hash & 0x0000000000800000) >> 21) |
            ((hash & 0x0000000000400000) >> 12) |
            ((hash & 0x0000000000200000) >> 3) |
            ((hash & 0x0000000000100000) << 6) |
            ((hash & 0x0000000000080000) << 15) |
            ((hash & 0x0000000000040000) << 24) |
            ((hash & 0x0000000000020000) << 33) |
            ((hash & 0x0000000000010000) << 42) |
            ((hash & 0x0000000000008000) >> 14) |
            ((hash & 0x0000000000004000) >> 5) |
            ((hash & 0x0000000000002000) << 4) |
            ((hash & 0x0000000000001000) << 13) |
            ((hash & 0x0000000000000800) << 22) |
            ((hash & 0x0000000000000400) << 31) |
            ((hash & 0x0000000000000200) << 40) |
            ((hash & 0x0000000000000100) << 49) |
            ((hash & 0x0000000000000080) >> 7) |
            ((hash & 0x0000000000000040) << 2) |
            ((hash & 0x0000000000000020) << 11) |
            ((hash & 0x0000000000000010) << 20) |
            ((hash & 0x0000000000000008) << 29) |
            ((hash & 0x0000000000000004) << 38) |
            ((hash & 0x0000000000000002) << 47) |
            ((hash & 0x0000000000000001) << 56);
    }
}
