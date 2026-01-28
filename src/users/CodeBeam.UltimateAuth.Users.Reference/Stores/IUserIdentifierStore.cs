using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserIdentifierStore
{
    Task<bool> ExistsAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default);

    Task<IReadOnlyList<UserIdentifier>> GetByUserAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

    Task<UserIdentifier?> GetAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default);

    Task CreateAsync(string? tenantId, UserIdentifier identifier, CancellationToken ct = default);

    Task UpdateValueAsync(string? tenantId, UserIdentifierType type, string oldValue, string newValue, DateTimeOffset updatedAt, CancellationToken ct = default);

    Task MarkVerifiedAsync(string? tenantId, UserIdentifierType type, string value, DateTimeOffset verifiedAt, CancellationToken ct = default);

    Task SetPrimaryAsync(string? tenantId, UserKey userKey, UserIdentifierType type, string value, CancellationToken ct = default);

    Task DeleteAsync(string? tenantId, UserIdentifierType type, string value, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);

    Task DeleteByUserAsync(string? tenantId, UserKey userKey, DeleteMode mode, DateTimeOffset deletedAt, CancellationToken ct = default);
}
