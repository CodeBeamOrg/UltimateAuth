using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
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
    private readonly IClock _clock;

    public InMemoryUserSeedContributor(
        IUserLifecycleStore lifecycle,
        IUserProfileStore profiles,
        IUserIdentifierStore identifiers,
        IInMemoryUserIdProvider<UserKey> ids,
        IClock clock)
    {
        _lifecycle = lifecycle;
        _profiles = profiles;
        _ids = ids;
        _identifiers = identifiers;
        _clock = clock;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        await SeedUserAsync(tenant, _ids.GetAdminUserId(), "Administrator", "admin", "admin@ultimateauth.com", "1234567890", ct);
        await SeedUserAsync(tenant, _ids.GetUserUserId(), "User", "user", "user@ultimateauth.com", "9876543210", ct);
    }

    private async Task SeedUserAsync(TenantKey tenant, UserKey userKey, string displayName, string primaryUsername,
        string primaryEmail, string primaryPhone, CancellationToken ct)
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
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Username,
                Value = primaryUsername,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _identifiers.CreateAsync(tenant,
            new UserIdentifier
            {
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Email,
                Value = primaryEmail,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);

        await _identifiers.CreateAsync(tenant,
            new UserIdentifier
            {
                Tenant = tenant,
                UserKey = userKey,
                Type = UserIdentifierType.Phone,
                Value = primaryPhone,
                IsPrimary = true,
                IsVerified = true,
                CreatedAt = _clock.UtcNow
            }, ct);
    }
}
