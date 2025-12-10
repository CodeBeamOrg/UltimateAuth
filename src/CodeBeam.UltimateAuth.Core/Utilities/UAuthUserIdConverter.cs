using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    /// <summary>
    /// Default implementation of <see cref="IUserIdConverter{TUserId}"/> that provides
    /// normalization and serialization for user identifiers.
    /// 
    /// Supports primitive types (<see cref="int"/>, <see cref="long"/>, <see cref="Guid"/>, <see cref="string"/>)
    /// with optimized formats. For custom types, JSON serialization is used as a safe fallback.
    /// 
    /// Converters are used throughout UltimateAuth for:
    /// - token generation
    /// - session-store keys
    /// - multi-tenancy boundaries
    /// - logging and diagnostics
    /// </summary>
    public sealed class UAuthUserIdConverter<TUserId> : IUserIdConverter<TUserId>
    {
        /// <summary>
        /// Converts the specified user id into a canonical string representation.
        /// Primitive types use invariant culture or compact formats; complex objects
        /// are serialized via JSON.
        /// </summary>
        /// <param name="id">The user identifier to convert.</param>
        /// <returns>A normalized string representation of the user id.</returns>
        public string ToString(TUserId id)
        {
            return id switch
            {
                int v => v.ToString(CultureInfo.InvariantCulture),
                long v => v.ToString(CultureInfo.InvariantCulture),
                Guid v => v.ToString("N"),
                string v => v,
                _ => JsonSerializer.Serialize(id)
            };
        }

        /// <summary>
        /// Converts the user id into UTF-8 encoded bytes derived from its
        /// normalized string representation.
        /// </summary>
        /// <param name="id">The user identifier to convert.</param>
        /// <returns>UTF-8 encoded bytes representing the user id.</returns>
        public byte[] ToBytes(TUserId id) => Encoding.UTF8.GetBytes(ToString(id));

        /// <summary>
        /// Converts a canonical string representation back into a user id.
        /// Supports primitives and restores complex types via JSON deserialization.
        /// </summary>
        /// <param name="value">The string representation of the user id.</param>
        /// <returns>The reconstructed user id.</returns>
        /// <exception cref="UAuthInternalException">
        /// Thrown when deserialization of complex types fails.
        /// </exception>
        public TUserId FromString(string value)
        {
            return typeof(TUserId) switch
            {
                Type t when t == typeof(int) => (TUserId)(object)int.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(long) => (TUserId)(object)long.Parse(value, CultureInfo.InvariantCulture),
                Type t when t == typeof(Guid) => (TUserId)(object)Guid.Parse(value),
                Type t when t == typeof(string) => (TUserId)(object)value,

                _ => JsonSerializer.Deserialize<TUserId>(value)
                     ?? throw new UAuthInternalException("Cannot deserialize TUserId")
            };
        }

        /// <summary>
        /// Converts a UTF-8 encoded binary representation back into a user id.
        /// </summary>
        /// <param name="binary">Binary data representing the user id.</param>
        /// <returns>The reconstructed user id.</returns>
        public TUserId FromBytes(byte[] binary) =>
            FromString(Encoding.UTF8.GetString(binary));

    }
}
