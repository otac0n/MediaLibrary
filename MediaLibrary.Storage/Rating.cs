namespace MediaLibrary.Storage
{
    public class Rating
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
