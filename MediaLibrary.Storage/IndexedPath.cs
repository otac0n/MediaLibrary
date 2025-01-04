// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using Microsoft.Extensions.FileSystemGlobbing;

    public class IndexedPath
    {
        public IndexedPath(string path, string include, string exclude)
            : this(path, null, include, exclude)
        {
            this.PathRaw = PathEncoder.GetPathRaw(path);
        }

        public IndexedPath(string path, byte[] pathRaw, string include, string exclude)
        {
            this.Path = PathEncoder.GetPath(path, pathRaw);
            this.PathRaw = pathRaw;
            this.Include = include;
            this.Exclude = exclude;

            var includeList = (include ?? string.Empty).Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (includeList.Length == 0)
            {
                includeList = new[] { "*" };
            }

            var excludeList = (exclude ?? string.Empty).Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            var matcher = new Matcher();
            matcher.AddIncludePatterns(includeList);
            matcher.AddExcludePatterns(excludeList);

            this.Matcher = matcher;
        }

        public string Include { get; }

        public string Exclude { get; }

        public string Path { get; }

        public byte[] PathRaw { get; }

        public Matcher Matcher { get; }

        internal static class Queries
        {
            public static readonly string AddIndexedPath = @"
                INSERT INTO IndexedPaths (Path, PathRaw, Include, Exclude) VALUES (@Path, @PathRaw, @Include, @Exclude)
            ";

            public static readonly string GetIndexedPaths = @"
                SELECT
                    Path,
                    PathRaw,
                    Include,
                    Exclude
                FROM IndexedPaths
            ";

            public static readonly string RemoveIndexedPath = @"
                DELETE FROM IndexedPaths
                WHERE Path = @Path
                AND ((@PathRaw IS NULL AND PathRaw IS NULL) OR (@PathRaw IS NOT NULL AND PathRaw = @PathRaw))
            ";
        }
    }
}
