// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using MediaLibrary.Search;
    using MediaLibrary.Storage.Search.Expressions;
    using MediaLibrary.Storage.Search.Optimizations;
    using TaggingLibrary;

    public abstract class SearchCompiler<T>
    {
        public static readonly string HiddenTag = "hidden";
        private static readonly Term ExcludeHiddenTerm = new NegationTerm(new FieldTerm("tag", FieldTerm.LessThanOrEqualOperator, HiddenTag));

        private static readonly ImmutableList<Optimization> Optimizations = ImmutableList.Create<Optimization>(
            new UnitaryLogicalOperatorOptimization(),
            new NestedLogicalOperatorOptimization(),
            new TagSetReductionOptimization(),
            new TagCombinationOptimization(),
            new RejectedTagSetReductionOptimization(),
            new RejectedTagCombinationOptimization(),
            new DeMorgansOptimization(),
            new DegenerateOptimization());

        private readonly ContainsHiddenReplacer containsHiddenReplacer;
        private readonly SearchDialect dialect;
        private readonly bool excludeHidden;

        public SearchCompiler(TagRuleEngine tagEngine, bool excludeHidden, Func<string, Term> getSavedSearch)
        {
            this.excludeHidden = excludeHidden;
            this.containsHiddenReplacer = new ContainsHiddenReplacer(tagEngine);
            this.dialect = new SearchDialect(tagEngine, getSavedSearch);
        }

        public T Compile(Term term)
        {
            var expression = this.dialect.Compile(term);

            // An empty search would usually return everything, but this is bad for performance and causes user anxiety.
            if (expression is ConjunctionExpression conjunction && conjunction.Expressions.Count == 0)
            {
                expression = new DisjunctionExpression(conjunction.Expressions);
            }
            else
            {
                // Hidden tags should be excluded unless explicitly requested.
                var containsHidden = this.containsHiddenReplacer.Replace(expression);
                if (this.excludeHidden & !containsHidden)
                {
                    expression = new ConjunctionExpression(ImmutableList.Create(
                        expression,
                        this.dialect.Compile(ExcludeHiddenTerm)));
                }
            }

            while (true)
            {
                var original = expression;

                foreach (var optimization in Optimizations)
                {
                    expression = optimization.Replace(expression);
                    if (!object.ReferenceEquals(expression, original))
                    {
                        break;
                    }
                }

                if (object.ReferenceEquals(expression, original))
                {
                    break;
                }
            }

            return this.Compile(expression);
        }

        protected abstract T Compile(Expression expression);

        private class ContainsHiddenReplacer : ExpressionReplacer<bool>
        {
            private readonly ImmutableHashSet<string> hiddenTags;

            public ContainsHiddenReplacer(TagRuleEngine tagEngine)
            {
                this.hiddenTags = tagEngine.GetTagDescendants(HiddenTag).Add(HiddenTag);
            }

            public override bool Replace(ConjunctionExpression expression) => expression.Expressions.Select(this.Replace).Any(c => c);

            public override bool Replace(DisjunctionExpression expression) => expression.Expressions.Select(this.Replace).Any(c => c);

            public override bool Replace(NegationExpression expression) => this.Replace(expression.Expression);

            public override bool Replace(CopiesExpression expression) => false;

            public override bool Replace(DetailsExpression expression) => false;

            public override bool Replace(FileSizeExpression expression) => false;

            public override bool Replace(HashExpression expression) => false;

            public override bool Replace(NoPeopleExpression expression) => false;

            public override bool Replace(PeopleCountExpression expression) => false;

            public override bool Replace(PersonIdExpression expression) => false;

            public override bool Replace(PersonNameExpression expression) => false;

            public override bool Replace(RatingExpression expression) => false;

            public override bool Replace(RatingsCountExpression expression) => false;

            public override bool Replace(StarsExpression expression) => false;

            public override bool Replace(TagExpression expression) => expression.Tags.Overlaps(this.hiddenTags);

            public override bool Replace(RejectedTagExpression expression) => expression.Tags.Overlaps(this.hiddenTags);

            public override bool Replace(TagCountExpression expression) => false;

            public override bool Replace(TextExpression expression) => false;

            public override bool Replace(TypeEqualsExpression expression) => false;

            public override bool Replace(TypePrefixExpression expression) => false;
        }
    }
}
