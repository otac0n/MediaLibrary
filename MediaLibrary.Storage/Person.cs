// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System.Collections.Immutable;

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

        public ImmutableHashSet<Alias> Aliases { get; set; }

        public string Name { get; set; }

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
                    Site,
                    Name
                FROM Alias;

                SELECT
                    PersonId,
                    Name
                FROM Person;
            ";

            public static readonly string GetPersonById = @"
                SELECT
                    PersonId,
                    Site,
                    Name
                FROM Alias
                WHERE PersonId = @PersonId;

                SELECT
                    PersonId,
                    Name
                FROM Person
                WHERE PersonId = @PersonId;
            ";

            public static readonly string MergePeople = @"
                INSERT OR REPLACE INTO Alias (PersonId, Site, Name)
                SELECT
                    @TargetId PersonId,
                    Site,
                    Name
                FROM Alias
                WHERE PersonId = @DuplicateId;

                INSERT OR REPLACE INTO Alias (PersonId, Site, Name)
                SELECT
                    @TargetId PersonId,
                    NULL Site,
                    Name
                FROM Person
                WHERE PersonId = @DuplicateId
                AND NOT (Name = (SELECT Name FROM Person WHERE PersonId = @TargetId));

                DELETE FROM Alias
                WHERE PersonId = @DuplicateId;

                INSERT OR REPLACE INTO HashPerson (Hash, PersonId)
                SELECT
                    Hash,
                    @TargetId PersonId
                FROM HashPerson
                WHERE PersonId = @DuplicateId;

                DELETE FROM HashPerson
                WHERE PersonId = @DuplicateId;

                DELETE FROM Person
                WHERE PersonId = @DuplicateId
            ";

            public static readonly string RemovePerson = @"
                DELETE FROM Alias WHERE PersonId = @PersonId
                DELETE FROM HashPerson WHERE PersonId = @PersonId
                DELETE FROM Person WHERE PersonId = @PersonId
            ";

            public static readonly string UpdatePerson = @"
                UPDATE Person SET Name = @Name WHERE PersonId = @PersonId;
            ";
        }
    }
}
