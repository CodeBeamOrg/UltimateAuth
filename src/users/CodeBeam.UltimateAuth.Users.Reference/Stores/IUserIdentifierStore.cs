using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStore
{
    Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default);

    Task<PagedResult<UserIdentifier>> QueryAsync(TenantKey tenant, UserIdentifierQuery query, CancellationToken ct = default);

    Task CreateAsync(TenantKey tenant, UserIdentifier identifier, CancellationToken ct = default);

    Task UpdateValueAsync(Guid id, string newRawValue, string newNormalizedValue, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task MarkVerifiedAsync(Guid id, DateTimeOffset verifiedAt, CancellationToken ct = default);

    Task SetPrimaryAsync(Guid id, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task UnsetPrimaryAsync(Guid id, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task DeleteAsync(Guid id, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);

    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
