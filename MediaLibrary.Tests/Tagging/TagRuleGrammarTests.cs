// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tests.Tagging
{
    using System.Linq;
    using MediaLibrary.Storage.Search;
    using MediaLibrary.Tagging;
    using Xunit;

    public class TagRuleGrammarTests
    {
        [Theory]
        [InlineData("a => b", TagOperator.Definition)]
        [InlineData("a -> b", TagOperator.Implication)]
        [InlineData("a <-> b", TagOperator.BidirectionalImplication)]
        [InlineData("a ~> b", TagOperator.Suggestion)]
        [InlineData("a <~> b", TagOperator.BidirectionalSuggestion)]
        [InlineData("a !> b", TagOperator.Exclusion)]
        [InlineData("a <!> b", TagOperator.MutualExclusion)]
        [InlineData("a :: b", TagOperator.Specialization)]
        public void Parse_WithASimpleRule_ReturnsTheExpectedOperator(string rule, TagOperator @operator)
        {
            var rules = new TagRulesGrammar().Parse(rule);
            var actual = rules.Single();
            Assert.Equal(@operator, actual.Operator);
        }
    }
}
