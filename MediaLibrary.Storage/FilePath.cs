// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System;

namespace MediaLibrary.Storage
{
    public class FilePath
    {
        public FilePath(string path, string lastHash, long lastModifiedTime, long? missingSince)
        {
            this.Path = path;
            this.LastHash = lastHash;
            this.LastModifiedTime = lastModifiedTime;
            this.MissingSince = missingSince;
        }

        public string LastHash { get; }

        public long LastModifiedTime { get; }

        public long? MissingSince { get; }

        public string Path { get; }

        public FilePath With(
            long lastModifiedTime,
            long? missingSince) =>
            new FilePath(
                this.Path,
                this.LastHash,
                lastModifiedTime,
                missingSince);

        public FilePath With(
            long lastModifiedTime) =>
            new FilePath(
                this.Path,
                this.LastHash,
                lastModifiedTime,
                this.MissingSince);

        public FilePath With(
            long? missingSince) =>
            new FilePath(
                this.Path,
                this.LastHash,
                this.LastModifiedTime,
                missingSince);

        internal static class Queries
        {
            public static readonly string AddFilePath = @"
                INSERT OR REPLACE INTO Paths (Path, LastHash, LastModifiedTime, MissingSince) VALUES (@Path, @LastHash, @LastModifiedTime, @MissingSince)
            ";

            public static readonly string GetFilePathByPath = @"
                SELECT
                    Path,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE Path = @Path
            ";

            public static readonly string GetFilePathsByHash = @"
                SELECT
                    Path,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE LastHash = @Hash
            ";

            public static readonly string GetFilePathsUnder = @"
                SELECT
                    Path,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE Path LIKE @Path || '%' ESCAPE '\'
            ";

            public static readonly string RemoveFilePathByPath = @"
                DELETE FROM Paths WHERE Path = @Path
            ";
        }
    }
}
