// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class PathComparer : IComparer<string>
    {
        public static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private PathComparer()
        {
        }

        public static PathComparer Instance { get; } = new PathComparer();

        public int Compare(string aPath, string bPath)
        {
            var aParts = aPath.ToUpperInvariant().Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            var bParts = bPath.ToUpperInvariant().Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            var num = 0;
            for (var j = 0; j < aParts.Length && j < bParts.Length; j++)
            {
                if (aParts.Length != bParts.Length)
                {
                    if (j == aParts.Length - 1)
                    {
                        return 1;
                    }
                    else if (j == bParts.Length - 1)
                    {
                        return -1;
                    }
                }

                if ((num = string.Compare(aParts[j], bParts[j], StringComparison.CurrentCultureIgnoreCase)) != 0)
                {
                    return num;
                }
            }

            return 0;
        }
    }
}
