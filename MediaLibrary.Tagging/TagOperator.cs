// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    /// <summary>
    /// Provides operators that can be performed between tags or sets of tags.
    /// </summary>
    public enum TagOperator
    {
        /// <summary>
        /// The left side is defined as the right side.
        /// </summary>
        Definition = 0,

        /// <summary>
        /// The left side implies the right side.
        /// </summary>
        Implication = 1,

        /// <summary>
        /// The left side and the right side imply each other.
        /// </summary>
        BidirectionalImplication = 2,

        /// <summary>
        /// The left side suggests the right side.
        /// </summary>
        Suggestion = 3,

        /// <summary>
        /// The left side and the right side suggest each other.
        /// </summary>
        BidirectionalSuggestion = 4,

        /// <summary>
        /// The left side excludes the right side.
        /// </summary>
        Exclusion = 5,

        /// <summary>
        /// The left side and right side are mutually exclusive.
        /// </summary>
        MutualExclusion = 6,

        /// <summary>
        /// The left side specializes the right side.
        /// </summary>
        Specialization = 7,
    }
}
