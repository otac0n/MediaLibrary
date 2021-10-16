// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search.Expressions
{
    using System;

    public abstract class ExpressionReplacer<TResult>
    {
        public abstract TResult Replace(ConjunctionExpression expression);

        public abstract TResult Replace(DisjunctionExpression expression);

        public abstract TResult Replace(NegationExpression expression);

        public abstract TResult Replace(CopiesExpression expression);

        public abstract TResult Replace(DetailsExpression expression);

        public abstract TResult Replace(FileSizeExpression expression);

        public abstract TResult Replace(HashExpression expression);

        public abstract TResult Replace(NoPeopleExpression expression);

        public abstract TResult Replace(PeopleCountExpression expression);

        public abstract TResult Replace(PersonIdExpression expression);

        public abstract TResult Replace(PersonNameExpression expression);

        public abstract TResult Replace(RatingExpression expression);

        public abstract TResult Replace(RatingsCountExpression expression);

        public abstract TResult Replace(StarsExpression expression);

        public abstract TResult Replace(TagExpression expression);

        public abstract TResult Replace(RejectedTagExpression expression);

        public abstract TResult Replace(TagCountExpression expression);

        public abstract TResult Replace(TextExpression expression);

        public abstract TResult Replace(TypeEqualsExpression expression);

        public abstract TResult Replace(TypePrefixExpression expression);

        public virtual TResult Replace(Expression expression)
        {
            switch (expression)
            {
                case ConjunctionExpression conjunctionExpression: return this.Replace(conjunctionExpression);
                case DisjunctionExpression disjunctionExpression: return this.Replace(disjunctionExpression);
                case NegationExpression negationExpression: return this.Replace(negationExpression);
                case CopiesExpression copiesExpression: return this.Replace(copiesExpression);
                case DetailsExpression detailsExpression: return this.Replace(detailsExpression);
                case FileSizeExpression fileSizeExpression: return this.Replace(fileSizeExpression);
                case HashExpression hashExpression: return this.Replace(hashExpression);
                case NoPeopleExpression noPeopleExpression: return this.Replace(noPeopleExpression);
                case PeopleCountExpression peopleCountExpression: return this.Replace(peopleCountExpression);
                case PersonIdExpression personIdExpression: return this.Replace(personIdExpression);
                case PersonNameExpression personNameExpression: return this.Replace(personNameExpression);
                case RatingExpression ratingExpression: return this.Replace(ratingExpression);
                case RatingsCountExpression ratingsCountExpression: return this.Replace(ratingsCountExpression);
                case StarsExpression starsExpression: return this.Replace(starsExpression);
                case TagExpression tagExpression: return this.Replace(tagExpression);
                case RejectedTagExpression rejectedTagExpression: return this.Replace(rejectedTagExpression);
                case TagCountExpression tagCountExpression: return this.Replace(tagCountExpression);
                case TextExpression textExpression: return this.Replace(textExpression);
                case TypeEqualsExpression typeEqualsExpression: return this.Replace(typeEqualsExpression);
                case TypePrefixExpression typePrefixExpression: return this.Replace(typePrefixExpression);
            }

            throw new NotSupportedException();
        }
    }
}
