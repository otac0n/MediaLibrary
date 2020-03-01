// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class HashTag
    {
        public HashTag(string hash, string tag)
        {
            this.Hash = hash;
            this.Tag = tag;
        }

        public string Hash { get; }

        public string Tag { get; }

        internal static class Queries
        {
            public static readonly string AddHashTag = @"
                INSERT OR REPLACE INTO HashTag (Hash, Tag) VALUES (@Hash, @Tag)
            ";

            public static readonly string GetHashTag = @"
                SELECT
                    Hash,
                    Tag
                FROM HashTag
                WHERE Hash = @Hash
            ";

            public static readonly string RemoveHashTag = @"
                DELETE FROM HashTag WHERE Hash = @Hash AND Tag = @Tag
            ";
        }
    }
}
