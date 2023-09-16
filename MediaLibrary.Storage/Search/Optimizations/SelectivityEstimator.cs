// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Optimizations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MediaLibrary.Search.Terms;
    using MediaLibrary.Storage.Search.Expressions;

    public class SelectivityEstimator : ExpressionReplacer<double>
    {
        protected const double DetailsSelectivity = 0.1;
        protected const double ExpectedCopies = 1.2;
        protected const double ExpectedFileSize = 4 * 1024 * 1024;
        protected const double ExpectedFileSizeVariance = 1024 * 1024;
        protected const double ExpectedPeopleCount = 1.5;
        protected const double ExpectedRatingsCount = 5;
        protected const double ExpectedTagCount = 5;
        protected const double IndividualPersonSelectivity = 0.01;
        protected const double NameSelectivity = 0.1;
        protected const double PerRejectedTagSelectivity = 0.0001;
        protected const double PerTagSelectivity = 0.001;
        protected const double TextSelectivity = 0.1;
        protected const double TypeEqualsSelectivity = 0.3;
        protected const double TypePrefixSelectivity = 0.5;

        private readonly Dictionary<Expression, double> cache = new Dictionary<Expression, double>();

        public override double Replace(Expression expression)
        {
            if (!this.cache.TryGetValue(expression, out var value))
            {
                this.cache[expression] = value = base.Replace(expression);
            }

            return value;
        }

        public override double Replace(ConjunctionExpression expression) =>
            expression.Expressions.Aggregate(1.0, (s, e) => s * this.Replace(e));

        public override double Replace(DisjunctionExpression expression) =>
            1 - expression.Expressions.Aggregate(1.0, (s, e) => s * (1.0 - this.Replace(e)));

        public override double Replace(NegationExpression expression) =>
            1 - this.Replace(expression.Expression);

        public override double Replace(CopiesExpression expression) =>
            AccumulateDiscreet(expression.Operator, expression.Copies, ExpectedCopies);

        public override double Replace(DetailsExpression expression) =>
            DetailsSelectivity;

        public override double Replace(FileSizeExpression expression) =>
            AccumulateContinuous(expression.Operator, expression.FileSize, ExpectedFileSize, ExpectedFileSizeVariance);

        public override double Replace(HashExpression expression)
        {
            const double HexSelectivity = 1 / 16.0;

            switch (expression.Operator)
            {
                case FieldTerm.EqualsOperator:
                    return Math.Pow(HexSelectivity, expression.Value.Length);
                case FieldTerm.UnequalOperator:
                    return 1 - Math.Pow(HexSelectivity, expression.Value.Length);
                case FieldTerm.ComparableOperator:
                    return 0;
            }

            int HexValue(char c)
            {
                if (c < '0')
                {
                    return 0;
                }
                else if (c <= '9')
                {
                    return c - '0';
                }
                else if (c < 'a')
                {
                    return 9;
                }
                else if (c <= 'f')
                {
                    return 10 + (c - 'a');
                }
                else
                {
                    return 15;
                }
            }

            var value = expression.Value.Select((c, i) =>
                Math.Pow(HexSelectivity, i) * HexValue(c)).Sum();

            switch (expression.Operator)
            {
                case FieldTerm.GreaterThanOperator:
                case FieldTerm.GreaterThanOrEqualOperator:
                    return 1 - value;

                case FieldTerm.LessThanOperator:
                case FieldTerm.LessThanOrEqualOperator:
                    return value;

                default:
                    throw new NotSupportedException($"Unrecognized operator '{expression.Operator}'.");
            }
        }

        public override double Replace(PeopleCountExpression expression) =>
            AccumulateDiscreet(expression.Operator, expression.PeopleCount, ExpectedPeopleCount);

        public override double Replace(PersonIdExpression expression) => IndividualPersonSelectivity;

        public override double Replace(PersonNameExpression expression) => NameSelectivity;

        public override double Replace(RatingExpression expression)
        {
            if (expression.Operator == FieldTerm.EqualsOperator && expression.Rating == Rating.DefaultRating)
            {
                return 0.5;
            }

            return AccumulateContinuous(expression.Operator, expression.Rating, Rating.DefaultRating, Rating.RatingScale);
        }

        public override double Replace(RatingsCountExpression expression) =>
            AccumulateDiscreet(expression.Operator, expression.RatingsCount, ExpectedRatingsCount);

        public override double Replace(StarsExpression expression) =>
            AccumulateDiscreet(expression.Operator, expression.Stars, i =>
            {
                switch (i)
                {
                    case 1: return 0.1;
                    case 2: return 0.2;
                    case 3: return 0.4;
                    case 4: return 0.2;
                    case 5: return 0.1;
                    default: return 0;
                }
            });

        public override double Replace(TagExpression expression) =>
            1 - Math.Pow(1 - PerTagSelectivity, expression.Tags.Count);

        public override double Replace(RejectedTagExpression expression) =>
            1 - Math.Pow(1 - PerRejectedTagSelectivity, expression.Tags.Count);

        public override double Replace(TagCountExpression expression) =>
            AccumulateDiscreet(expression.Operator, expression.TagCount, ExpectedTagCount);

        public override double Replace(TextExpression expression) =>
            TextSelectivity;

        public override double Replace(TypeEqualsExpression expression) =>
            TypeEqualsSelectivity;

        public override double Replace(TypePrefixExpression expression) =>
            TypePrefixSelectivity;

        protected static double AccumulateContinuous(string fieldOperator, double value, double mean, double scale)
        {
            switch (fieldOperator)
            {
                case FieldTerm.EqualsOperator:
                    return 1 / scale;
                case FieldTerm.UnequalOperator:
                    return 1 - 1 / scale;
                case FieldTerm.ComparableOperator:
                    return 0;
            }

            var sum = CumulativeLogistic(mean, scale, value);

            switch (fieldOperator)
            {
                case FieldTerm.GreaterThanOperator:
                case FieldTerm.GreaterThanOrEqualOperator:
                    return 1 - sum;

                case FieldTerm.LessThanOperator:
                case FieldTerm.LessThanOrEqualOperator:
                    return sum;

                default:
                    throw new NotSupportedException($"Unrecognized operator '{fieldOperator}'.");
            }
        }

        protected static double AccumulateDiscreet(string fieldOperator, int value, double mean) =>
            AccumulateDiscreet(fieldOperator, value, i => Poisson(mean, i));

        protected static double AccumulateDiscreet(string fieldOperator, int value, Func<int, double> probability)
        {
            int upper;
            bool invert;
            switch (fieldOperator)
            {
                case FieldTerm.EqualsOperator:
                    return probability(value);

                case FieldTerm.UnequalOperator:
                    return 1 - probability(value);

                case FieldTerm.ComparableOperator:
                    return 0;

                case FieldTerm.GreaterThanOperator:
                    upper = value;
                    invert = true;
                    break;

                case FieldTerm.GreaterThanOrEqualOperator:
                    upper = value - 1;
                    invert = true;
                    break;

                case FieldTerm.LessThanOperator:
                    upper = value - 1;
                    invert = false;
                    break;

                case FieldTerm.LessThanOrEqualOperator:
                    upper = value;
                    invert = false;
                    break;

                default:
                    throw new NotSupportedException($"Unrecognized operator '{fieldOperator}'.");
            }

            var sum = 0.0;
            for (var i = 0; i <= upper; i++)
            {
                sum += probability(i);
            }

            return invert ? 1 - sum : sum;
        }

        protected static double CumulativeLogistic(double mean, double scale, double value) =>
            0.5 + 0.5 * Math.Tanh((value - mean) / (2 * scale));

        protected static double Poisson(double mean, int value) =>
            value < 0 ? 0 : Math.Pow(mean, value) * Math.Pow(Math.E, -mean) / Factorial(value);

        private static double Factorial(int n) =>
            n == 0 ? 1 : Math.Ceiling(Math.Sqrt(2 * Math.PI * n) * Math.Pow(n / Math.E, n));
    }
}
