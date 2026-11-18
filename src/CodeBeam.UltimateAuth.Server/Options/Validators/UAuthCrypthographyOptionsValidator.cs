using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCryptographyOptionsValidator(string environmentName, bool usesBuiltInSigning, bool usesBuiltInHashing) : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var failures = new List<string>();
        var crypto = options.Cryptography;

        if (crypto is null)
        {
            return ValidateOptionsResult.Fail("Cryptography options must not be null.");
        }

        byte[]? signingKey = null;
        byte[]? hashingKey = null;

        if (usesBuiltInSigning)
        {
            signingKey = ValidateKey(crypto.JwtSigningKey, nameof(crypto.JwtSigningKey), failures);
        }

        if (usesBuiltInHashing)
        {
            hashingKey = ValidateKey(crypto.TokenHashingKey, nameof(crypto.TokenHashingKey), failures);
        }

        if (signingKey is not null && hashingKey is not null)
        {
            try
            {
                UAuthKeyMaterial.EnsureDistinct(signingKey, hashingKey);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(ex.Message);
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private byte[]? ValidateKey(string? value, string name, List<string> failures)
    {
        byte[] key;

        try
        {
            key = UAuthKeyMaterial.Decode(value, $"Cryptography.{name}");
        }
        catch (InvalidOperationException ex)
        {
            failures.Add(ex.Message);
            return null;
        }

        if (!string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) &&
            (MatchesDevelopmentKey(key, UAuthCryptographyConstants.DevelopmentJwtSigningKey) ||
             MatchesDevelopmentKey(key, UAuthCryptographyConstants.DevelopmentTokenHashingKey)))
        {
            failures.Add($"Cryptography.{name} cannot use a known UltimateAuth development key outside Development.");
        }

        return key;
    }

    private static bool MatchesDevelopmentKey(byte[] key, string developmentKey)
    {
        var expected = Convert.FromBase64String(developmentKey);

        return key.Length == expected.Length && CryptographicOperations.FixedTimeEquals(key, expected);
    }
}
