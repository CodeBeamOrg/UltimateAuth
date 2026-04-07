using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

// TODO: Add rehashing support (rehash on login if options have changed, or if hash is malformed). This is important to ensure that password hashes stay up-to-date with the latest security standards and configurations. It also allows for seamless upgrades to the hashing algorithm or parameters without forcing users to reset their passwords.
namespace CodeBeam.UltimateAuth.Security.Argon2;

internal sealed class Argon2PasswordHasher : IUAuthPasswordHasher
{
    private readonly Argon2Options _options;

    public Argon2PasswordHasher(IOptions<Argon2Options> options)
    {
        _options = options.Value;
    }

    public PasswordHash Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new UAuthValidationException("Password cannot be null or empty.");

        var salt = RandomNumberGenerator.GetBytes(_options.SaltSize);
        var argon2 = CreateArgon2(password, salt);
        var hash = argon2.GetBytes(_options.HashSize);

        var encoded = $"{_options.Iterations}.{_options.MemorySizeKb}.{_options.Parallelism}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        return PasswordHash.Create(PasswordAlgorithms.Argon2, encoded);
    }

    public bool Verify(PasswordHash hash, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(hash.Hash))
            return false;

        if (hash.Algorithm != PasswordAlgorithms.Argon2)
            return false;

        var parts = hash.Hash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            return false;

        if (!int.TryParse(parts[0], out var iterations) ||
            !int.TryParse(parts[1], out var memory) ||
            !int.TryParse(parts[2], out var parallelism))
                    return false;

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);

        try
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(secret))
            {
                Salt = salt,
                Iterations = iterations,
                MemorySize = memory,
                DegreeOfParallelism = parallelism
            };

            var actualHash = argon2.GetBytes(expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(PasswordHash hash)
    {
        if (hash.Algorithm != PasswordAlgorithms.Argon2)
            return true;

        var parts = hash.Hash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            return true;

        if (!int.TryParse(parts[0], out var iterations) ||
            !int.TryParse(parts[1], out var memory) ||
            !int.TryParse(parts[2], out var parallelism))
                    return true;

        return iterations != _options.Iterations ||
               memory != _options.MemorySizeKb ||
               parallelism != _options.Parallelism;
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
