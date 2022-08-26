// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class PersonTag
    {
        public PersonTag(int personId, string tag)
        {
            this.PersonId = personId;
            this.Tag = tag;
        }

        public PersonTag(long personId, string tag)
        {
            this.PersonId = checked((int)personId);
            this.Tag = tag;
        }

        public int PersonId { get; }

        public string Tag { get; }

        internal static class Queries
        {
            public static readonly string AddPersonTag = @"
                DELETE FROM RejectedPersonTags WHERE PersonId = @PersonId AND Tag = @Tag;
                INSERT OR REPLACE INTO PersonTag (PersonId, Tag) VALUES (@PersonId, @Tag)
            ";

            public static readonly string GetAllPersonTags = @"
                SELECT DISTINCT Tag FROM PersonTag
            ";

            public static readonly string GetPersonTags = @"
                SELECT
                    PersonId,
                    Tag
                FROM PersonTag
                WHERE PersonId = @PersonId
            ";

            public static readonly string GetRejectedPersonTags = @"
                SELECT
                    PersonId,
                    Tag
                FROM RejectedPersonTags
                WHERE PersonId = @PersonId
            ";

            public static readonly string RejectPersonTag = @"
                DELETE FROM PersonTag WHERE PersonId = @PersonId AND Tag = @Tag;
                INSERT OR REPLACE INTO RejectedPersonTags (PersonId, Tag) VALUES (@PersonId, @Tag)
            ";

            public static readonly string RemovePersonTag = @"
                DELETE FROM PersonTag WHERE PersonId = @PersonId AND Tag = @Tag
            ";
        }
    }
}
