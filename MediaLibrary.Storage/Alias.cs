// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class Alias
    {
        public Alias(int personId, string site, string name)
        {
            this.Site = site;
            this.Name = name;
            this.PersonId = personId;
        }

        public Alias(long personId, string site, string name)
            : this(checked((int)personId), site, name)
        {
        }

        public string Name { get; }

        public int PersonId { get; }

        public string Site { get; }

        /// <inheritdoc/>
        public override string ToString() =>
            string.IsNullOrEmpty(this.Site)
                ? $"\"{this.Name}\""
                : $"\"{this.Name}\" on {this.Site}";

        internal static class Queries
        {
            public static readonly string AddAlias = @"
                DELETE FROM Alias WHERE @Site IS NOT NULL AND Site = @Site AND Name = @Name;
                INSERT INTO Alias (PersonId, Site, Name) VALUES (@PersonId, @Site, @Name);
                SELECT PersonId, Site, Name FROM Alias WHERE PersonId = @PersonId AND Name = @Name AND (Site = @Site OR (@Site IS NULL AND Site IS NULL));
            ";

            public static readonly string GetAliasesByPersonId = @"
                SELECT
                    PersonId,
                    Site,
                    Name
                FROM Alias
                WHERE PersonId = @PersonId
            ";

            public static readonly string GetAliasesBySite = @"
                SELECT
                    PersonId,
                    Site,
                    Name
                FROM Alias
                WHERE Site = @Site
            ";

            public static readonly string GetAliasesBySiteAndName = @"
                SELECT
                    PersonId,
                    Site,
                    Name
                FROM Alias
                WHERE Name = @Name AND (Site = @Site OR (@Site IS NULL AND Site IS NULL))
            ";

            public static readonly string GetAllSites = @"
                SELECT DISTINCT
                    Site
                FROM Alias
                WHERE Site IS NOT NULL
            ";

            public static readonly string RemoveAlias = @"
                DELETE FROM Alias WHERE Name = @Name AND (Site = @Site OR (@Site IS NULL AND Site IS NULL)) AND PersonId = @PersonId
            ";
        }
    }
}
