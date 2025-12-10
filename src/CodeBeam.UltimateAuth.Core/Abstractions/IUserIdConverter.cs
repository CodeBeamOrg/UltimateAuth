namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Defines conversion logic for transforming user identifiers between
    /// strongly typed values, string representations, and binary formats.
    /// Implementations enable consistent storage, token serialization,
    /// and multitenant key partitioning.
    /// </summary>
    public interface IUserIdConverter<TUserId>
    {
        /// <summary>
        /// Converts the typed user identifier into its canonical string representation.
        /// </summary>
        /// <param name="id">The user identifier to convert.</param>
        /// <returns>A stable and reversible string representation of the identifier.</returns>
        string ToString(TUserId id);

        /// <summary>
        /// Converts the typed user identifier into a binary representation suitable for efficient storage or hashing operations.
        /// </summary>
        /// <param name="id">The user identifier to convert.</param>
        /// <returns>A byte array representing the identifier.</returns>
        byte[] ToBytes(TUserId id);

        /// <summary>
        /// Reconstructs a typed user identifier from its string representation.
        /// </summary>
        /// <param name="value">The string-encoded identifier.</param>
        /// <returns>The reconstructed user identifier.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the input value cannot be parsed into a valid identifier.
        /// </exception>
        TUserId FromString(string value);

        /// <summary>
        /// Reconstructs a typed user identifier from its binary representation.
        /// </summary>
        /// <param name="binary">The byte array containing the encoded identifier.</param>
        /// <returns>The reconstructed user identifier.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the input binary value cannot be parsed into a valid identifier.
        /// </exception>
        TUserId FromBytes(byte[] binary);
    }
}
