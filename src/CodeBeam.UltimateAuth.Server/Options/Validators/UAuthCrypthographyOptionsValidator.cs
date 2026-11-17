using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
            signingKey = ValidateKey(crypto.JwtSigningKey, nameof(crypto.JwtSigningKey), UAuthCryptographyConstants.DevelopmentJwtSigningKey, failures);
        }

        if (usesBuiltInHashing)
        {
            hashingKey = ValidateKey(crypto.TokenHashingKey, nameof(crypto.TokenHashingKey), UAuthCryptographyConstants.DevelopmentTokenHashingKey, failures);
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

    private byte[]? ValidateKey(string? value, string name, string developmentDefault, List<string> failures)
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
            (string.Equals(value, UAuthCryptographyConstants.DevelopmentJwtSigningKey, StringComparison.Ordinal) ||
            string.Equals(value, UAuthCryptographyConstants.DevelopmentTokenHashingKey, StringComparison.Ordinal)))
        {
            failures.Add($"Cryptography.{name} cannot use a known UltimateAuth development key outside Development.");
        }

        return key;
    }
}
