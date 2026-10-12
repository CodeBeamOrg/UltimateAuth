using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;
using static MudBlazor.CategoryTypes;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public sealed class InMemoryUserSummaryQueryStoreContractTests : UserSummaryQueryStoreContractTests
{
    protected override Task<IUserSummaryQueryStoreTestDatabase> CreateDatabaseAsync()
    {
        return Task.FromResult<IUserSummaryQueryStoreTestDatabase>(new Database());
    }

    private sealed class Database : IUserSummaryQueryStoreTestDatabase
    {
        private readonly InMemoryAtomicContextAccessor _atomic = new();

        private readonly Dictionary<TenantKey, TenantStores> _stores = [];

        private readonly IUAuthPaginationPolicy _pagination = new UAuthPaginationOptions();

        private sealed record TenantStores(
            IUserLifecycleStore Lifecycles,
            IUserProfileStore Profiles,
            IUserIdentifierStore Identifiers);


        private TenantStores GetStores(TenantKey tenant)
        {
            if (_stores.TryGetValue(tenant, out var existing))
                return existing;

            var context = new TenantExecutionContext(tenant);

            var stores = new TenantStores(
                new InMemoryUserLifecycleStore(
                    context,
                    _atomic,
                    _pagination),
                new InMemoryUserProfileStore(
                    context,
                    _atomic, _pagination),
                new InMemoryUserIdentifierStore(
                    context,
                    _atomic, _pagination));

            _stores.Add(tenant, stores);
            return stores;
        }


        public IUserSummaryQueryStore CreateStore(TenantKey tenant)
        {
            var stores = GetStores(tenant);

            return new InMemoryUserSummaryQueryStore(
                stores.Lifecycles,
                stores.Profiles,
                stores.Identifiers,
                _pagination);
        }

        public async Task SeedAsync(
            TenantKey tenant,
            IReadOnlyList<UserSummarySeed> users)
        {
            var stores = GetStores(tenant);

            foreach (var user in users)
            {
                var lifecycle = UserLifecycle.Create(
                    tenant,
                    user.UserKey,
                    user.CreatedAt);

                if (user.Status != UserStatus.Active)
                {
                    lifecycle.ChangeStatus(
                        user.CreatedAt,
                        user.Status);
                }

                if (user.IsDeleted)
                {
                    lifecycle.MarkDeleted(
                        user.CreatedAt.AddMinutes(1));
                }

                await stores.Lifecycles.AddAsync(lifecycle);

                if (user.HasProfile)
                {
                    var profile = UserProfile.Create(
                        id: null,
                        tenant: tenant,
                        userKey: user.UserKey,
                        profileKey: user.ProfileKey,
                        createdAt: user.CreatedAt,
                        displayName: user.DisplayName);

                    await stores.Profiles.AddAsync(profile);
                }

                await AddIdentifierAsync(
                    stores.Identifiers, tenant, user,
                    UserIdentifierType.Username,
                    user.UserName);

                await AddIdentifierAsync(
                    stores.Identifiers, tenant, user,
                    UserIdentifierType.Email,
                    user.Email);

                await AddIdentifierAsync(
                    stores.Identifiers, tenant, user,
                    UserIdentifierType.Phone,
                    user.Phone);
            }
        }

        private static async Task AddIdentifierAsync(
            IUserIdentifierStore store,
            TenantKey tenant,
            UserSummarySeed user,
            UserIdentifierType type,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var identifier = UserIdentifier.Create(
                id: null,
                tenant: tenant,
                userKey: user.UserKey,
                type: type,
                value: value,
                normalizedValue: value.ToLowerInvariant(),
                now: user.CreatedAt,
                isPrimary: true);

            await store.AddAsync(identifier);
        }

        public ValueTask DisposeAsync()
        {
            _stores.Clear();
            return ValueTask.CompletedTask;
        }
    }
}
