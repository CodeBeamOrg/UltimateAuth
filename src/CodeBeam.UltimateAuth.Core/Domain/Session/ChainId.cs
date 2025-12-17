namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents a strongly typed identifier for a session chain.
    /// A session chain groups multiple rotated sessions belonging to the same
    /// device or application family, providing type safety across the UltimateAuth session system.
    /// </summary>
    public readonly struct ChainId : IEquatable<ChainId>
    {
        /// <summary>
        /// Initializes a new <see cref="ChainId"/> with the specified GUID value.
        /// </summary>
        /// <param name="value">The underlying GUID representing the chain identifier.</param>
        public ChainId(Guid value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the underlying GUID value of the chain identifier.
        /// </summary>
        public Guid Value { get; }

        /// <summary>
        /// Generates a new chain identifier using a newly created GUID.
        /// </summary>
        /// <returns>A new <see cref="ChainId"/> instance.</returns>
        public static ChainId New() => new ChainId(Guid.NewGuid());

        /// <summary>
        /// Determines whether the specified <see cref="ChainId"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The chain identifier to compare with.</param>
        /// <returns><c>true</c> if both identifiers represent the same chain.</returns>
        public bool Equals(ChainId other) => Value.Equals(other.Value);

        /// <summary>
        /// Determines whether the specified object is equal to the current chain identifier.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if the object is a <see cref="ChainId"/> with the same value.</returns>
        public override bool Equals(object? obj) => obj is ChainId other && Equals(other);

        public static bool operator ==(ChainId left, ChainId right) => left.Equals(right);

        public static bool operator !=(ChainId left, ChainId right) => !left.Equals(right);

        /// <summary>
        /// Returns a hash code based on the underlying GUID value.
        /// </summary>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Returns the string representation of the underlying GUID value.
        /// </summary>
        /// <returns>The GUID as a string.</returns>
        public override string ToString() => Value.ToString();

        /// <summary>
        /// Converts the <see cref="ChainId"/> to its underlying <see cref="Guid"/> value.
        /// </summary>
        /// <param name="id">The chain identifier.</param>
        /// <returns>The underlying GUID value.</returns>
        public static implicit operator Guid(ChainId id) => id.Value;
    }
}
