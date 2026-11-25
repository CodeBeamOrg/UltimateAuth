using System.Text;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthJwtValidator : IUAuthJwtValidator
{
    private const string AccessTokenType = "uauth-access+jwt";

    private readonly IJwtVerificationKeyProvider _keys;
    private readonly UAuthJwtValidationOptions _options;
    private readonly JsonWebTokenHandler _handler = new();

    public UAuthJwtValidator(IJwtVerificationKeyProvider keys, IOptions<UAuthJwtValidationOptions> options)
    {
        _keys = keys;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Issuer) ||
            string.IsNullOrWhiteSpace(_options.Audience) ||
            _options.ValidAlgorithms is null ||
            _options.ValidAlgorithms.Count == 0 ||
            _options.ClockSkew < TimeSpan.Zero ||
            _options.MaximumTokenSizeInBytes <= 0)
        {
            throw new InvalidOperationException("JWT validation configuration is invalid.");
        }
    }

    public async Task<ValidatedJwtIdentity?> ValidateAsync(string token, TenantKey expectedTenant, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (Encoding.UTF8.GetByteCount(token) > _options.MaximumTokenSizeInBytes)
        {
            return null;
        }

        if (!expectedTenant.IsValid || !JwtPayloadGuard.IsValid(token))
        {
            return null;
        }

        if (!_handler.CanReadToken(token))
            return null;

        JsonWebToken jwt;

        try
        {
            jwt = _handler.ReadJsonWebToken(token);
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (jwt.Typ != AccessTokenType ||
            string.IsNullOrWhiteSpace(jwt.Kid) ||
            !_options.ValidAlgorithms.Contains(jwt.Alg, StringComparer.Ordinal))
        {
            return null;
        }

        var matchingKeys = _keys.GetVerificationKeys()
            .Where(key => string.Equals(key.KeyId, jwt.Kid, StringComparison.Ordinal) && IsCompatibleKey(key, jwt.Alg))
            .ToArray();

        if (matchingKeys.Length == 0)
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,

            ValidateAudience = true,
            ValidAudience = _options.Audience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = _options.ClockSkew,

            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = matchingKeys,

            ValidAlgorithms = _options.ValidAlgorithms,

            ValidTypes = new[] { AccessTokenType },

            TryAllIssuerSigningKeys = false
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);

        ct.ThrowIfCancellationRequested();

        if (!result.IsValid || result.SecurityToken is not JsonWebToken validated)
        {
            return null;
        }

        var subjectClaims = validated.Claims
            .Where(x => x.Type == "sub")
            .ToArray();

        var tenantClaims = validated.Claims
            .Where(x => x.Type == "tenant")
            .ToArray();

        if (subjectClaims.Length != 1 || tenantClaims.Length != 1)
        {
            return null;
        }

        var subject = subjectClaims[0].Value;
        var tenant = tenantClaims[0].Value;

        if (string.IsNullOrWhiteSpace(subject))
            return null;

        if (!string.Equals(tenant, expectedTenant.Value, StringComparison.Ordinal))
        {
            return null;
        }

        if (validated.ValidTo == DateTime.MinValue)
            return null;

        return new ValidatedJwtIdentity
        {
            Subject = subject,
            TenantId = tenant,
            Issuer = validated.Issuer,
            Audience = _options.Audience,
            ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(validated.ValidTo, DateTimeKind.Utc)),
            Claims = validated.Claims.ToArray()
        };
    }

    private static bool IsCompatibleKey(SecurityKey key, string algorithm)
    {
        return algorithm switch
        {
            SecurityAlgorithms.HmacSha256 or
            SecurityAlgorithms.HmacSha384 or
            SecurityAlgorithms.HmacSha512
                => key is SymmetricSecurityKey,

            SecurityAlgorithms.RsaSha256 or
            SecurityAlgorithms.RsaSha384 or
            SecurityAlgorithms.RsaSha512 or
            SecurityAlgorithms.RsaSsaPssSha256 or
            SecurityAlgorithms.RsaSsaPssSha384 or
            SecurityAlgorithms.RsaSsaPssSha512
                => key is RsaSecurityKey or X509SecurityKey,

            SecurityAlgorithms.EcdsaSha256 or
            SecurityAlgorithms.EcdsaSha384 or
            SecurityAlgorithms.EcdsaSha512
                => key is ECDsaSecurityKey,

            _ => false
        };
    }
}
