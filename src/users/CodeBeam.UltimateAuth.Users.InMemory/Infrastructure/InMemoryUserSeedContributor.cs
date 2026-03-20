using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSeedContributor : ISeedContributor
{
    public int Order => 0;

    private readonly IUserLifecycleStoreFactory _lifecycleFactory;
    private readonly IUserIdentifierStoreFactory _identifierFactory;
    private readonly IUserProfileStoreFactory _profileFactory;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IIdentifierNormalizer _identifierNormalizer;
    private readonly IClock _clock;

    public InMemoryUserSeedContributor(
        IUserLifecycleStoreFactory lifecycleFactory,
        IUserProfileStoreFactory profileFactory,
        IUserIdentifierStoreFactory identifierFactory,
        IInMemoryUserIdProvider<UserKey> ids,
        IIdentifierNormalizer identifierNormalizer,
        IClock clock)
    {
        _lifecycleFactory = lifecycleFactory;
        _identifierFactory = identifierFactory;
        _profileFactory = profileFactory;
        _ids = ids;
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
        var now = _clock.UtcNow;

        var lifecycleStore = _lifecycleFactory.Create(tenant);
        var profileStore = _profileFactory.Create(tenant);
        var identifierStore = _identifierFactory.Create(tenant);

        var lifecycleKey = new UserLifecycleKey(tenant, userKey);

        var exists = await lifecycleStore.ExistsAsync(lifecycleKey, ct);

        if (!exists)
        {
            await lifecycleStore.AddAsync(
                UserLifecycle.Create(tenant, userKey, now),
                ct);
        }

        var profileKey = new UserProfileKey(tenant, userKey);
        if (!await profileStore.ExistsAsync(profileKey, ct))
        {
            await profileStore.AddAsync(
                UserProfile.Create(Guid.NewGuid(), tenant, userKey, now, displayName: displayName),
                ct);
        }

        async Task EnsureIdentifier(
            UserIdentifierType type,
            string value,
            bool isPrimary)
        {
            var normalized = _identifierNormalizer
                .Normalize(type, value).Normalized;

            var existing = await identifierStore.GetAsync(type, normalized, ct);

            if (existing is not null)
                return;

            await identifierStore.AddAsync(
                UserIdentifier.Create(
                    Guid.NewGuid(),
                    tenant,
                    userKey,
                    type,
                    value,
                    normalized,
                    now,
                    isPrimary,
                    now),
                ct);
        }

        await EnsureIdentifier(UserIdentifierType.Username, username, true);
        await EnsureIdentifier(UserIdentifierType.Email, email, true);
        await EnsureIdentifier(UserIdentifierType.Phone, phone, true);
    }
}
