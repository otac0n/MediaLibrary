// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System.Collections.Immutable;
    using MediaLibrary.Storage.Search.Expressions;
    using MediaLibrary.Storage.Search.Optimizations;

    public abstract class SearchCompiler<T>
    {
        private static readonly ImmutableList<Optimization> Optimizations = ImmutableList.Create<Optimization>(
            new UnitaryLogicalOperatorOptimization(),
            new NestedLogicalOperatorOptimization(),
            new TagSetReductionOptimization(),
            new TagCombinationOptimization(),
            new RejectedTagSetReductionOptimization(),
            new RejectedTagCombinationOptimization(),
            new DeMorgansOptimization(),
            new DegenerateOptimization());

        public T CompileQuery(Expression expression)
        {
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
    }
}
