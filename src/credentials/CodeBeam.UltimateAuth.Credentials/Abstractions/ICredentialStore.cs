using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialStore
{
    Task<IReadOnlyCollection<ICredential>>GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<ICredential?> GetByIdAsync(TenantKey tenant, Guid credentialId, CancellationToken ct = default);
    Task AddAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default);
    Task UpdateAsync(TenantKey tenant, ICredential credential, long expectedVersion, CancellationToken ct = default);
    Task RevokeAsync(TenantKey tenant, Guid credentialId, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default);
    Task DeleteAsync(TenantKey tenant, Guid credentialId, DeleteMode mode, DateTimeOffset now, long expectedVersion, CancellationToken ct = default);
    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default);
}
