using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Users.Reference;
public sealed class LoginIdentifierResolver : ILoginIdentifierResolver
{
    private readonly IUserIdentifierStore _store;
    private readonly IIdentifierNormalizer _normalizer;
    private readonly IEnumerable<ICustomLoginIdentifierResolver> _customResolvers;
    private readonly UAuthLoginIdentifierOptions _options;

    public LoginIdentifierResolver(
        IUserIdentifierStore store,
        IIdentifierNormalizer normalizer,
        IEnumerable<ICustomLoginIdentifierResolver> customResolvers,
        IOptions<UAuthServerOptions> options)
    {
        _store = store;
        _normalizer = normalizer;
        _customResolvers = customResolvers;
        _options = options.Value.LoginIdentifiers;
    }

    public async Task<LoginIdentifierResolution?> ResolveAsync(TenantKey tenant, string identifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var raw = identifier;

        var builtInType = DetectBuiltInType(identifier);

        var normalizedResult = _normalizer.Normalize(builtInType, identifier);

        if (!normalizedResult.IsValid)
            return null;

        var normalized = normalizedResult.Normalized;


        if (_options.EnableCustomResolvers && _options.CustomResolversFirst)
        {
            var custom = await TryCustomAsync(tenant, normalized, ct);
            if (custom is not null)
                return custom;
        }

        if (!_options.AllowedTypes.Contains(builtInType))
        {
            if (_options.EnableCustomResolvers && !_options.CustomResolversFirst)
                return await TryCustomAsync(tenant, normalized, ct);

            return null;
        }

        var found = await _store.GetAsync(tenant, builtInType, normalized, ct);
        if (found is null || !found.IsPrimary)
        {
            if (_options.EnableCustomResolvers && !_options.CustomResolversFirst)
                return await TryCustomAsync(tenant, normalized, ct);

            return new LoginIdentifierResolution
            {
                Tenant = tenant,
                RawIdentifier = raw,
                NormalizedIdentifier = normalized,
                BuiltInType = builtInType,
                UserKey = null,
                IsVerified = false
            };
        }

        if (builtInType == UserIdentifierType.Email && _options.RequireVerificationForEmail && !found.IsVerified)
            return new LoginIdentifierResolution
            {
                Tenant = tenant,
                RawIdentifier = raw,
                NormalizedIdentifier = normalized,
                BuiltInType = builtInType,
                UserKey = null,
                IsVerified = false
            };

        if (builtInType == UserIdentifierType.Phone && _options.RequireVerificationForPhone && !found.IsVerified)
            return new LoginIdentifierResolution
            {
                Tenant = tenant,
                RawIdentifier = raw,
                NormalizedIdentifier = normalized,
                BuiltInType = builtInType,
                UserKey = null,
                IsVerified = false
            };

        return new LoginIdentifierResolution
        {
            Tenant = tenant,
            RawIdentifier = raw,
            NormalizedIdentifier = normalized,
            BuiltInType = builtInType,
            UserKey = found.UserKey,
            IsVerified = found.IsVerified
        };
    }

    private async Task<LoginIdentifierResolution?> TryCustomAsync(TenantKey tenant, string normalizedIdentifier, CancellationToken ct)
    {
        foreach (var r in _customResolvers)
        {
            if (!r.CanResolve(normalizedIdentifier))
                continue;

            var result = await r.ResolveAsync(tenant, normalizedIdentifier, ct);
            if (result is not null)
                return result;
        }

        return null;
    }

    private static UserIdentifierType DetectBuiltInType(string normalized)
    {
        if (normalized.Contains('@', StringComparison.Ordinal))
            return UserIdentifierType.Email;

        var digits = 0;
        foreach (var ch in normalized)
        {
            if (char.IsDigit(ch)) digits++;
            else if (ch is '+' or '-' or ' ' or '(' or ')') { }
            else return UserIdentifierType.Username;
        }

        if (digits >= 7)
            return UserIdentifierType.Phone;

        return UserIdentifierType.Username;
    }
}
