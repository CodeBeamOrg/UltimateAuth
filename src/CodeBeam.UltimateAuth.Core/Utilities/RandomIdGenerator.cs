using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Core.Utilities
{
    public static class RandomIdGenerator
    {
        /// <summary>
        /// Generates a secure random ID with the specified byte length,
        /// encoded as Base64Url.
        /// </summary>
        public static string Generate(int byteLength)
        {
            if (byteLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));

            var buffer = new byte[byteLength];

            RandomNumberGenerator.Fill(buffer);

            return Base64Url.Encode(buffer);
        }

        /// <summary>
        /// Generates cryptographically secure raw bytes.
        /// </summary>
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
