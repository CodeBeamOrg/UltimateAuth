using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStore : IVersionedStore<UserIdentifier, Guid>
{
    Task<IdentifierExistenceResult> ExistsAsync(IdentifierExistenceQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(UserKey userKey, CancellationToken ct = default);
    Task<UserIdentifier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserIdentifier?> GetAsync(UserIdentifierType type, string value, CancellationToken ct = default);

    Task<PagedResult<UserIdentifier>> QueryAsync(UserIdentifierQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUsersAsync(IReadOnlyList<UserKey> userKeys, CancellationToken ct = default);
    Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
