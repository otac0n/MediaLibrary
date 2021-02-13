// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class HashPerson
    {
        public HashPerson(string hash, int personId)
        {
            this.Hash = hash;
            this.PersonId = personId;
        }

        public string Hash { get; }

        public int PersonId { get; }

        internal static class Queries
        {
            public static readonly string AddHashPerson = @"
                DELETE FROM RejectedPerson WHERE Hash = @Hash AND PersonId = @PersonId;
                INSERT OR REPLACE INTO HashPerson (Hash, PersonId) VALUES (@Hash, @PersonId)
            ";

            public static readonly string GetHashPeople = @"
                SELECT
                    Hash,
                    PersonId
                FROM HashPerson
                WHERE Hash = @Hash
            ";

            public static readonly string RejectHashPerson = @"
                DELETE FROM HashPerson WHERE Hash = @Hash AND PersonId = @PersonId;
                INSERT OR REPLACE INTO RejectedPerson (Hash, PersonId) VALUES (@Hash, @PersonId)
            ";

            public static readonly string RemoveHashPerson = @"
                DELETE FROM HashPerson WHERE Hash = @Hash AND PersonId = @PersonId
            ";
        }
    }
}
