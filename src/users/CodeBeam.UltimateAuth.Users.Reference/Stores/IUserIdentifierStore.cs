using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStore : IVersionedStore<UserIdentifier, Guid>
{
    Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserIdentifier?> GetAsync(TenantKey tenant, UserIdentifierType type, string value, CancellationToken ct = default);

    Task<PagedResult<UserIdentifier>> QueryAsync(TenantKey tenant, UserIdentifierQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUsersAsync(TenantKey tenant, IReadOnlyList<UserKey> userKeys, CancellationToken ct = default);
    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
