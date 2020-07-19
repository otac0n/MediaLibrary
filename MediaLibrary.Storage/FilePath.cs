// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Text;

    public class FilePath
    {
        public FilePath(string path, string lastHash, long lastModifiedTime, long? missingSince)
            : this(path, null, lastHash, lastModifiedTime, missingSince)
        {
            this.PathRaw = FilePath.GetPathRaw(path);
        }

        public FilePath(string path, byte[] pathRaw, string lastHash, long lastModifiedTime, long? missingSince)
        {
            this.Path = pathRaw == null ? path : PathEncoder.Decode(pathRaw);
            this.PathRaw = pathRaw;
            this.LastHash = lastHash;
            this.LastModifiedTime = lastModifiedTime;
            this.MissingSince = missingSince;
        }

        public string LastHash { get; }

        public long LastModifiedTime { get; }

        public long? MissingSince { get; }

        public string Path { get; }

        public byte[] PathRaw { get; }

        public FilePath With(
            long lastModifiedTime,
            long? missingSince) =>
            new FilePath(
                this.Path,
                this.PathRaw,
                this.LastHash,
                lastModifiedTime,
                missingSince);

        public FilePath With(
            long lastModifiedTime) =>
            new FilePath(
                this.Path,
                this.PathRaw,
                this.LastHash,
                lastModifiedTime,
                this.MissingSince);

        public FilePath With(
            long? missingSince) =>
            new FilePath(
                this.Path,
                this.PathRaw,
                this.LastHash,
                this.LastModifiedTime,
                missingSince);

        internal static byte[] GetPathRaw(string path) =>
            path != Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(path))
                ? PathEncoder.Encode(path)
                : null;

        internal static class Queries
        {
            public static readonly string AddFilePath = @"
                INSERT OR REPLACE INTO Paths (Path, PathRaw, LastHash, LastModifiedTime, MissingSince) VALUES (@Path, @PathRaw, @LastHash, @LastModifiedTime, @MissingSince)
            ";

            public static readonly string GetFilePathByPath = @"
                SELECT
                    Path,
                    PathRaw,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE Path = @Path
                AND ((@PathRaw IS NULL AND PathRaw IS NULL) OR (@PathRaw IS NOT NULL AND PathRaw = @PathRaw))
            ";

            public static readonly string GetFilePathsByHash = @"
                SELECT
                    Path,
                    PathRaw,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE LastHash = @Hash
            ";

            public static readonly string GetFilePathsUnder = @"
                DROP TABLE IF EXISTS temp.GetFilePathsUnder;
                CREATE TEMP TABLE temp.GetFilePathsUnder (Path text, PathRaw blob, LastHash text, LastModifiedTime INTEGER, MissingSince INTEGER);
                INSERT INTO temp.GetFilePathsUnder (Path, PathRaw, LastHash, LastModifiedTime, MissingSince)
                SELECT
                    Path,
                    PathRaw,
                    LastHash,
                    LastModifiedTime,
                    MissingSince
                FROM Paths
                WHERE Path LIKE @Path || '%' ESCAPE '\';

                SELECT
                    Hash,
                    FileSize,
                    FileType,
                    CASE WHEN Hash IN (SELECT Hash FROM HashDetails) THEN TRUE ELSE FALSE END AS HasHashDetails
                FROM HashInfo
                WHERE Hash IN (SELECT LastHash FROM temp.GetFilePathsUnder);

                SELECT * FROM temp.GetFilePathsUnder;
                DROP TABLE temp.GetFilePathsUnder
            ";

            public static readonly string RemoveFilePathByPath = @"
                DELETE FROM Paths
                WHERE Path = @Path
                AND ((@PathRaw IS NULL AND PathRaw IS NULL) OR (@PathRaw IS NOT NULL AND PathRaw = @PathRaw))
            ";
        }
    }
}
