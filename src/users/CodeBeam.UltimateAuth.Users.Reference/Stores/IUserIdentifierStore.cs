using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStore
{
    Task<bool> ExistsAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

    Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default);

    Task CreateAsync(TenantKey tenant, UserIdentifier identifier, CancellationToken ct = default);

    Task UpdateValueAsync(TenantKey tenant, UserIdentifierType type, string oldValue, string newValue, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task MarkVerifiedAsync(TenantKey tenant, UserIdentifierType type, string value, DateTimeOffset verifiedAt, CancellationToken ct = default);

    Task SetPrimaryAsync(TenantKey tenant, UserKey userKey, UserIdentifierType type, string value, CancellationToken ct = default);

    Task UnsetPrimaryAsync(TenantKey tenant, UserKey userKey, UserIdentifierType type, string value, CancellationToken ct = default);

    Task DeleteAsync(TenantKey tenant, UserIdentifierType type, string value, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);

    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
