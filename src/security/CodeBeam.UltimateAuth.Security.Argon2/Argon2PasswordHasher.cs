using System.Security.Cryptography;
using System.Text;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Security.Argon2;

internal sealed class Argon2PasswordHasher : IUAuthPasswordHasher
{
    private readonly Argon2Options _options;

    public Argon2PasswordHasher(IOptions<Argon2Options> options)
    {
        _options = options.Value;
    }

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new UAuthValidationException("Password cannot be null or empty.");

        var salt = RandomNumberGenerator.GetBytes(_options.SaltSize);

        var argon2 = CreateArgon2(password, salt);

        var hash = argon2.GetBytes(_options.HashSize);

        // format:
        // {salt}.{hash}
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string hash, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(hash))
            return false;

        var parts = hash.Split('.');
        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);

            var argon2 = CreateArgon2(secret, salt);
            var actualHash = argon2.GetBytes(expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private Argon2id CreateArgon2(string password, byte[] salt)
    {
        return new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = _options.Parallelism,
            Iterations = _options.Iterations,
            MemorySize = _options.MemorySizeKb
        };
    }
}
