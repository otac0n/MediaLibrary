// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class HashInfo
    {
        public HashInfo(string hash, long fileSize, string fileType)
        {
            this.Hash = hash;
            this.FileSize = fileSize;
            this.FileType = fileType;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        internal static class Queries
        {
            public static readonly string AddHashInfo = @"
                INSERT OR REPLACE INTO HashInfo (Hash, FileSize, FileType) VALUES (@Hash, @FileSize, @FileType)
            ";

            public static readonly string GetHashInfo = @"
                SELECT
                    Hash,
                    FileSize,
                    FileType
                FROM HashInfo
                WHERE Hash = @Hash
            ";
        }
    }
}
