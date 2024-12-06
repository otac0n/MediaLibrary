namespace MediaLibrary.Storage
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Rating : IComparable<Rating>
    {
        public static readonly double DefaultRating = 1500;
        public static readonly double RatingScale = 400.0;
        public static readonly long ProvisionalPeriod = 15;
        public static readonly Comparer<Rating> Comparer = Comparer<Rating>.Create(Rating.Compare);

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

        public static void ApplyScore(double actualScore, ref Rating left, ref Rating right)
        {
            var expectedScore = Rating.GetExpectedScore(left, right);
            var averageCount = (left.Count + right.Count) / 2.0;
            var k = 15 + 15.0 / (0.14 * averageCount + 1);

            left = new Rating(
                left.Hash,
                left.Category,
                left.Value + k * (expectedScore - actualScore),
                left.Count + 1);

            right = new Rating(
                right.Hash,
                right.Category,
                right.Value + k * (actualScore - expectedScore),
                right.Count + 1);
        }

        public static int Compare(Rating a, Rating b)
        {
            var value = (b?.Value ?? Rating.DefaultRating).CompareTo(a?.Value ?? Rating.DefaultRating);
            if (value == 0)
            {
                value = (b?.Count ?? 0).CompareTo(a?.Count ?? 0);
            }

            return value;
        }

        public static double GetExpectedScore(double leftValue, double rightValue) =>
            1.0 / (1.0 + Math.Pow(10.0, (leftValue - rightValue) / Rating.RatingScale));

        public static double GetExpectedScore(Rating left, Rating right) =>
            Rating.GetExpectedScore(
                left?.Value ?? Rating.DefaultRating,
                right?.Value ?? Rating.DefaultRating);

        public int CompareTo(Rating other) => Rating.Compare(this, other);

        public override string ToString()
        {
            var rounded = Math.Round(this.Value);
            return this.Count < ProvisionalPeriod ? $"{rounded}?" : $"{rounded}";
        }

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

            public static readonly string GetRatingStarRanges = @"
                SELECT
                    Stars,
                    CASE WHEN Stars <= 1 THEN NULL ELSE MIN(Value) END Min,
                    CASE WHEN Stars >= 5 THEN NULL ELSE MAX(Value) END Max
                    FROM (
                        SELECT Value, CASE WHEN NTile < 2 THEN 1 WHEN NTile < 4 THEN 2 WHEN NTile < 8 THEN 3 WHEN NTile < 10 THEN 4 ELSE 5 END AS Stars FROM (
                            SELECT Hash, Value, Count, NTILE(10) OVER (PARTITION BY Category ORDER BY Value) NTile
                            FROM Rating
                            WHERE Category = ''
                        ) r
                ) s
                GROUP BY Stars
                ORDER BY Stars
            ";
        }
    }
}
