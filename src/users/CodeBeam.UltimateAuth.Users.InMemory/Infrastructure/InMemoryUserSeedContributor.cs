using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
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

        public async Task SeedAsync(string? tenantId, CancellationToken ct = default)
        {
            await SeedUserAsync(tenantId, _ids.GetAdminUserId(), "Administrator", "admin", ct);
            await SeedUserAsync(tenantId, _ids.GetUserUserId(), "User", "user", ct);
        }

        private async Task SeedUserAsync(string? tenantId, UserKey userKey, string displayName, string username, CancellationToken ct)
        {
            if (await _lifecycle.ExistsAsync(tenantId, userKey, ct))
                return;

            await _lifecycle.CreateAsync(tenantId,
                new UserLifecycle
                {
                    UserKey = userKey,
                    Status = UserStatus.Active,
                    CreatedAt = _clock.UtcNow
                }, ct);

            await _profiles.CreateAsync(tenantId,
                new UserProfile
                {
                    UserKey = userKey,
                    DisplayName = displayName,
                    CreatedAt = _clock.UtcNow
                }, ct);

            await _identifiers.CreateAsync(tenantId,
                new UserIdentifier
                {
                    UserKey = userKey,
                    Type = UserIdentifierType.Username,
                    Value = username,
                    IsPrimary = true,
                    IsVerified = true,
                    CreatedAt = _clock.UtcNow
                }, ct);
        }
    }

}
