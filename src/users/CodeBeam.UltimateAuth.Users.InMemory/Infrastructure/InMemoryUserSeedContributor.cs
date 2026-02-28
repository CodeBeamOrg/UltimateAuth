using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSeedContributor : ISeedContributor
{
    public int Order => 0;

    private readonly IUserLifecycleStore _lifecycle;
    private readonly IUserProfileStore _profiles;
    private readonly IUserIdentifierStore _identifiers;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IIdentifierNormalizer _identifierNormalizer;
    private readonly IClock _clock;

    public InMemoryUserSeedContributor(
        IUserLifecycleStore lifecycle,
        IUserProfileStore profiles,
        IUserIdentifierStore identifiers,
        IInMemoryUserIdProvider<UserKey> ids,
        IIdentifierNormalizer identifierNormalizer,
        IClock clock)
    {
        _lifecycle = lifecycle;
        _profiles = profiles;
        _ids = ids;
        _identifiers = identifiers;
        _identifierNormalizer = identifierNormalizer;
        _clock = clock;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        await SeedUserAsync(tenant, _ids.GetAdminUserId(), "Administrator", "admin", "admin@ultimateauth.com", "1234567890", ct);
        await SeedUserAsync(tenant, _ids.GetUserUserId(), "Standard User", "user", "user@ultimateauth.com", "9876543210", ct);
    }

    private async Task SeedUserAsync(TenantKey tenant, UserKey userKey, string displayName, string username, string email, string phone, CancellationToken ct)
    {
        if (await _lifecycle.ExistsAsync(tenant, userKey, ct))
            return;

        await _lifecycle.CreateAsync(tenant,
            new UserLifecycle
            {
                Tenant = tenant,
                UserKey = userKey,
                Status = UserStatus.Active,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _profiles.CreateAsync(tenant,
            new UserProfile
            {
                Tenant = tenant,
                UserKey = userKey,
                DisplayName = displayName,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _identifiers.CreateAsync(tenant,
            new UserIdentifier
            {
                Id = Guid.NewGuid(),
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Username,
                Value = username,
                NormalizedValue = _identifierNormalizer
                    .Normalize(UserIdentifierType.Username, username)
                    .Normalized,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _identifiers.CreateAsync(tenant,
            new UserIdentifier
            {
                Id = Guid.NewGuid(),
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Email,
                Value = email,
                NormalizedValue = _identifierNormalizer
                    .Normalize(UserIdentifierType.Email, email)
                    .Normalized,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _identifiers.CreateAsync(tenant,
            new UserIdentifier
            {
                Id = Guid.NewGuid(),
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Phone,
                Value = phone,
                NormalizedValue = _identifierNormalizer
                    .Normalize(UserIdentifierType.Phone, phone)
                    .Normalized,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);
    }
}
