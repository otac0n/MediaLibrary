namespace MediaLibrary.Storage
{
    using System;

    public class Rating : IComparable<Rating>
    {
        public static readonly double DefaultRating = 1500;

        public Rating(string hash, string category, double value, long count)
        {
            this.Hash = hash;
            this.Category = category ?? string.Empty;
            this.Value = value;
            this.Count = count;
        }

        public string Category { get; }

        public long Count { get; }

        public string Hash { get; }

        public double Value { get; }

        public static int Compare(Rating a, Rating b)
        {
            var value = (b?.Value ?? Rating.DefaultRating).CompareTo(a?.Value ?? Rating.DefaultRating);
            if (value == 0)
            {
                value = (b?.Count ?? 0).CompareTo(a?.Count ?? 0);
            }

            return value;
        }

        public int CompareTo(Rating other) => Rating.Compare(this, other);

        internal static class Queries
        {
            public static readonly string GetRating = @"
                SELECT
                    Hash,
                    Category,
                    Value,
                    Count
                FROM Rating
                WHERE Hash = @Hash
                AND Category = @Category
            ";

            public static readonly string GetRatingCategories = @"
                SELECT DISTINCT Category
                FROM Rating
                WHERE Category <> ''
            ";

            public static readonly string UpdateRating = @"
                INSERT OR REPLACE INTO Rating (Hash, Category, Value, Count) VALUES (@Hash, @Category, @Value, @Count)
            ";
        }
    }
}
