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
                DELETE FROM RejectedTags WHERE Hash = @Hash AND Tag = @Tag;
                INSERT OR REPLACE INTO HashTag (Hash, Tag) VALUES (@Hash, @Tag)
            ";

            public static readonly string GetAllTags = @"
                SELECT DISTINCT Tag FROM HashTag
            ";

            public static readonly string GetHashTags = @"
                SELECT
                    Hash,
                    Tag
                FROM HashTag
                WHERE Hash = @Hash
            ";

            public static readonly string GetRejectedTags = @"
                SELECT
                    Hash,
                    Tag
                FROM RejectedTags
                WHERE Hash = @Hash
            ";

            public static readonly string RejectHashTag = @"
                DELETE FROM HashTag WHERE Hash = @Hash AND Tag = @Tag;
                INSERT OR REPLACE INTO RejectedTags (Hash, Tag) VALUES (@Hash, @Tag)
            ";

            public static readonly string RemoveHashTag = @"
                DELETE FROM HashTag WHERE Hash = @Hash AND Tag = @Tag
            ";
        }
    }
}
