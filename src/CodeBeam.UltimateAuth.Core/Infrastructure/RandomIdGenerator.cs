using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    /// <summary>
    /// Provides cryptographically secure random ID generation.
    /// 
    /// Produces opaque identifiers suitable for session IDs, PKCE codes,
    /// refresh tokens, and other entropy-critical values. Output is encoded
    /// using Base64Url for safe transport in URLs and headers.
    /// </summary>
    public static class RandomIdGenerator
    {
        /// <summary>
        /// Generates a cryptographically secure random identifier with the
        /// specified byte length and returns it as a URL-safe Base64 string.
        /// </summary>
        /// <param name="byteLength">The number of random bytes to generate.</param>
        /// <returns>A URL-safe Base64 encoded random value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="byteLength"/> is zero or negative.
        /// </exception>
        public static string Generate(int byteLength)
        {
            if (byteLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));

            var buffer = new byte[byteLength];
            RandomNumberGenerator.Fill(buffer);

            return Base64Url.Encode(buffer);
        }

        /// <summary>
        /// Generates a cryptographically secure random byte array with the
        /// specified length.
        /// </summary>
        /// <param name="byteLength">The number of bytes to generate.</param>
        /// <returns>A randomly filled byte array.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="byteLength"/> is zero or negative.
        /// </exception>
        public static byte[] GenerateBytes(int byteLength)
        {
            if (byteLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));

            var buffer = new byte[byteLength];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }

    }
}
