// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a possible value that can be either a reference or value type.
    /// </summary>
    /// <remarks>This is similar to <see cref="Nullable{T}"/>, but supports reference types.</remarks>
    /// <typeparam name="T">The type of the possible value.</typeparam>
    public struct Maybe<T> : IEquatable<Maybe<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Maybe{T}"/> struct with the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public Maybe(T value)
        {
            this.HasValue = true;
            this.ValueOrDefault = value;
        }

        /// <summary>
        /// Gets a value indicating whether or not there is a value.
        /// </summary>
        public bool HasValue { get; private set; }

        /// <summary>
        /// Gets the possible value.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no value.</exception>
        public T Value
        {
            get
            {
                if (!this.HasValue)
                {
                    throw new InvalidOperationException();
                }

                return this.ValueOrDefault;
            }
        }

        /// <summary>
        /// Gets the possible value, or the defalut value of <typeparamref name="T"/>, if there is no value.
        /// </summary>
        public T ValueOrDefault { get; private set; }

        /// <summary>
        /// Implicitly constructs a <see cref="Maybe{T}"/> value given an existing value.
        /// </summary>
        /// <param name="value">The value.</param>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "This implicit operator is itself an alternate name for the constructor.")]
        public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);

        /// <summary>
        /// Compares two <see cref="Maybe{T}"/> objects. The result specifies whether they are unequal.
        /// </summary>
        /// <param name="left">The first <see cref="Maybe{T}"/> to compare.</param>
        /// <param name="right">The second <see cref="Maybe{T}"/> to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> differ; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Maybe<T> left, Maybe<T> right) => !(left == right);

        /// <summary>
        /// Compares two <see cref="Maybe{T}"/> objects. The result specifies whether they are equal.
        /// </summary>
        /// <param name="left">The first <see cref="Maybe{T}"/> to compare.</param>
        /// <param name="right">The second <see cref="Maybe{T}"/> to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Maybe<T> other && this.Equals(other);

        /// <inheritdoc />
        public bool Equals(Maybe<T> other) =>
            other.HasValue == this.HasValue &&
            (!other.HasValue || object.Equals(other.ValueOrDefault, this.ValueOrDefault));

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.HasValue ? this.ValueOrDefault?.GetHashCode() ?? 0 : -1;
        }
    }
}
