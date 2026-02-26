using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialStore
{
    Task<IReadOnlyCollection<ICredential>>GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
    Task<ICredential?> GetByIdAsync(TenantKey tenant, Guid credentialId, CancellationToken ct = default);
    Task AddAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default);
    Task UpdateAsync(TenantKey tenant, ICredential credential, CancellationToken ct = default);
    Task RevokeAsync(TenantKey tenant, Guid credentialId, DateTimeOffset revokedAt, CancellationToken ct = default);
    Task DeleteAsync(TenantKey tenant, Guid credentialId, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default);
    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default);
    Task<bool> ExistsAsync(TenantKey tenant, UserKey userKey, CredentialType type, string? secretHash, CancellationToken ct = default);
}
