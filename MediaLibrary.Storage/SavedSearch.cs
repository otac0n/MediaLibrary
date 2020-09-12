// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public sealed class SavedSearch
    {
        public SavedSearch(int searchId, string name, string query)
        {
            this.SearchId = searchId;
            this.Name = name;
            this.Query = query;
        }

        public SavedSearch(long searchId, string name, string query)
            : this(checked((int)searchId), name, query)
        {
        }

        public string Name { get; }

        public string Query { get; }

        public int SearchId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{this.Name}:{this.SearchId}: {this.Query}";

        internal static class Queries
        {
            public static readonly string AddSavedSearch = @"
                INSERT INTO SavedSearch (Name, Query) VALUES (@Name, @Query);
                SELECT last_insert_rowid() AS SearchId, @Name AS Name, @Query AS Query;
            ";

            public static readonly string GetSavedSearches = @"
                SELECT
                    SearchId,
                    Name,
                    Query
                FROM SavedSearch
            ";

            public static readonly string RemoveSavedSearch = @"
                DELETE FROM SavedSearch WHERE SearchId = @SearchId
            ";

            public static readonly string UpdateSavedSearch = @"
                UPDATE SavedSearch SET Name = @Name, Query = @Query WHERE SearchId = @SearchId
            ";
        }
    }
}
