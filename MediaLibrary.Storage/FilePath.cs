// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class FilePath
    {
        public FilePath(string path, string lastHash, long lastModifiedTime)
        {
            this.Path = path;
            this.LastHash = lastHash;
            this.LastModifiedTime = lastModifiedTime;
        }

        public string LastHash { get; }

        public long LastModifiedTime { get; }

        public string Path { get; }

        internal static class Queries
        {
            public static readonly string AddFilePath = @"
                INSERT OR REPLACE INTO Paths (Path, LastHash, LastModifiedTime) VALUES (@Path, @LastHash, @LastModifiedTime)
            ";

            public static readonly string GetFilePathByPath = @"
                SELECT
                    Path,
                    LastHash,
                    LastModifiedTime
                FROM Paths
                WHERE Path = @Path
            ";

            public static readonly string GetFilePathsByHash = @"
                SELECT
                    Path,
                    LastHash,
                    LastModifiedTime
                FROM Paths
                WHERE LastHash = @Hash
            ";

            public static readonly string RemoveFilePathByPath = @"
                DELETE FROM Paths WHERE Path = @Path
            ";
        }
    }
}
