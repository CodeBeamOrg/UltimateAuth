namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents a strongly typed identifier for an authentication session.
    /// Wraps a <see cref="Guid"/> value and provides type safety across the UltimateAuth session management system.
    /// </summary>
    public readonly struct AuthSessionId : IEquatable<AuthSessionId>
    {
        /// <summary>
        /// Initializes a new <see cref="AuthSessionId"/> using the specified GUID value.
        /// </summary>
        /// <param name="value">The underlying GUID representing the session identifier.</param>
        public AuthSessionId(Guid value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the underlying GUID value of the session identifier.
        /// </summary>
        public Guid Value { get; }

        /// <summary>
        /// Generates a new session identifier using a newly created GUID.
        /// </summary>
        /// <returns>A new <see cref="AuthSessionId"/> instance.</returns>
        public static AuthSessionId New() => new AuthSessionId(Guid.NewGuid());

        /// <summary>
        /// Determines whether the specified <see cref="AuthSessionId"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The session identifier to compare with.</param>
        /// <returns><c>true</c> if the identifiers match; otherwise, <c>false</c>.</returns>
        public bool Equals(AuthSessionId other) => Value.Equals(other.Value);

        /// <summary>
        /// Determines whether the specified object is equal to the current session identifier.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if the object is an <see cref="AuthSessionId"/> with the same value.</returns>
        public override bool Equals(object? obj) => obj is AuthSessionId other && Equals(other);

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
        /// Converts the <see cref="AuthSessionId"/> to its underlying <see cref="Guid"/>.
        /// </summary>
        /// <param name="id">The session identifier.</param>
        /// <returns>The underlying GUID value.</returns>
        public static implicit operator Guid(AuthSessionId id) => id.Value;
    }
}
