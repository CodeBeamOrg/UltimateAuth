using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserIdentifierStore
    {
        Task<IReadOnlyCollection<UserIdentifier>> GetAllAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
        Task<IReadOnlyCollection<UserIdentifier>> GetByTypeAsync(string? tenantId, UserKey userKey, UserIdentifierType type, CancellationToken ct = default);
        Task SetAsync(string? tenantId, UserKey userKey, UserIdentifier record, CancellationToken ct = default);
        Task MarkVerifiedAsync(string? tenantId, UserKey userKey, UserIdentifierType type, DateTimeOffset verifiedAt, CancellationToken ct = default);
        Task<bool> ExistsAsync(string? tenantId, UserIdentifierType type, string value, CancellationToken ct = default);
        Task DeleteAsync(string? tenantId, UserKey userKey, UserIdentifierType type, string value, DeleteMode mode, CancellationToken ct = default);
    }
}
