// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class Person
    {
        public Person(int personId, string name)
        {
            this.PersonId = personId;
            this.Name = name;
        }

        public Person(long personId, string name)
        {
            this.PersonId = checked((int)personId);
            this.Name = name;
        }

        public string Name { get; }

        public int PersonId { get; }

        public override string ToString() => this.Name;

        internal static class Queries
        {
            public static readonly string AddPerson = @"
                INSERT OR REPLACE INTO Person (Name) VALUES (@Name);
                SELECT last_insert_rowid() AS PersonId, @Name AS Name;
            ";

            public static readonly string GetAllPeople = @"
                SELECT
                    PersonId,
                    Name
                FROM Person
            ";

            public static readonly string GetPersonById = @"
                SELECT
                    PersonId,
                    Name
                FROM Person
                WHERE PersonId = @PersonId
            ";

            public static readonly string RemovePerson = @"
                DELETE FROM Person WHERE PersonId = @PersonId
            ";

            public static readonly string UpdatePerson = @"
                UPDATE Person SET Name = @Name WHERE PersonId = @PersonId;
                SELECT PersonId, Name FROM Person WHERE PersonId = @PersonId;
            ";
        }
    }
}
