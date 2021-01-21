// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class RuleCategory
    {
        public RuleCategory(string category, long order, string rules)
        {
            this.Category = category;
            this.Order = order;
            this.Rules = rules;
        }

        public string Category { get; }

        public long Order { get; }

        public string Rules { get; }

        internal static class Queries
        {
            public static readonly string AddRuleCategory = @"
                INSERT INTO TagRules (Category, [Order], Rules) VALUES (@Category, @Order, @Rules);
            ";

            public static readonly string ClearRuleCategories = @"
                DELETE FROM TagRules;
            ";

            public static readonly string GetAllRuleCategories = @"
                SELECT
                    Category,
                    [Order],
                    Rules
                FROM TagRules
                ORDER BY [Order]
            ";
        }
    }
}
