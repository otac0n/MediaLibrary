// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using MediaLibrary.Search.Terms;
    using TaggingLibrary;

    public static class TagDialect
    {
        public static ImmutableDictionary<string, HierarchyRelation> TagRelationships = ImmutableDictionary.CreateRange(new Dictionary<string, HierarchyRelation>
        {
            [FieldTerm.GreaterThanOperator] = HierarchyRelation.Ancestor,
            [FieldTerm.EqualsOperator] = HierarchyRelation.Self,
            [FieldTerm.GreaterThanOrEqualOperator] = HierarchyRelation.SelfOrAncestor,
            [FieldTerm.LessThanOperator] = HierarchyRelation.Descendant,
            [FieldTerm.LessThanOrEqualOperator] = HierarchyRelation.SelfOrDescendant,
        });
    }
}
