// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System;

namespace MediaLibrary.Storage
{
    public class HashInfo
    {
        public HashInfo(string hash, long fileSize, string fileType, long version)
        {
            this.Hash = hash;
            this.FileSize = fileSize;
            this.FileType = fileType;
            this.Version = version;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        public long Version { get; }

        internal static class Queries
        {
            public static readonly string AddHashInfo = @"
                INSERT OR REPLACE INTO HashInfo (Hash, FileSize, FileType, Version) VALUES (@Hash, @FileSize, @FileType, @Version)
            ";

            public static readonly string GetHashInfo = @"
                SELECT
                    Hash,
                    FileSize,
                    FileType,
                    Version
                FROM HashInfo
                WHERE Hash = @Hash
            ";
        }
    }
}
