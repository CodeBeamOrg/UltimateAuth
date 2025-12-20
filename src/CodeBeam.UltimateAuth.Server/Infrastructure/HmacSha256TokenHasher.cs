using CodeBeam.UltimateAuth.Core.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class HmacSha256TokenHasher : ITokenHasher
    {
        private readonly byte[] _key;

        public HmacSha256TokenHasher(byte[] key)
        {
            if (key is null || key.Length == 0)
                throw new ArgumentException("Token hashing key must be provided.", nameof(key));

            _key = key;
        }

        public string Hash(string plaintext)
        {
            using var hmac = new HMACSHA256(_key);
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var hash = hmac.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool Verify(string plaintext, string hash)
        {
            var computed = Hash(plaintext);

            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computed),
                Convert.FromBase64String(hash));
        }
    }

}
