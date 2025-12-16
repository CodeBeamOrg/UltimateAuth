namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    /// <summary>
    /// Provides Base64 URL-safe encoding and decoding utilities.
    /// 
    /// RFC 4648-compliant transformation replacing '+' → '-', '/' → '_'
    /// and removing padding characters '='. Commonly used in PKCE,
    /// JWT segments, and opaque token representations.
    /// </summary>
    public static class Base64Url
    {
        /// <summary>
        /// Encodes a byte array into a URL-safe Base64 string by applying
        /// RFC 4648 URL-safe transformations and removing padding.
        /// </summary>
        /// <param name="input">The binary data to encode.</param>
        /// <returns>A URL-safe Base64 encoded string.</returns>
        public static string Encode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        /// <summary>
        /// Decodes a URL-safe Base64 string into its original binary form.
        /// Automatically restores required padding before decoding.
        /// </summary>
        /// <param name="input">The URL-safe Base64 encoded string.</param>
        /// <returns>The decoded binary data.</returns>
        public static byte[] Decode(string input)
        {
            var padded = input
                .Replace("-", "+")
                .Replace("_", "/");

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Convert.FromBase64String(padded);
        }

    }
}
