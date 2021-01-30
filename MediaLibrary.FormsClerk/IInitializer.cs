// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a generic runtime initializer.
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// Gets the accessor function that will create new instances of the item.
        /// </summary>
        Func<object[], object> Accessor { get; }

        /// <summary>
        /// Gets the name of the initializer.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a collection of parameters to be passed to the accessor function as arguments.
        /// </summary>
        IReadOnlyList<Parameter> Parameters { get; }
    }
}
