using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials;

public interface ICredentialStore<TUserId>
{
    Task<IReadOnlyCollection<ICredential<TUserId>>>FindByLoginAsync(TenantKey tenant, string loginIdentifier, CancellationToken ct = default);
    Task<IReadOnlyCollection<ICredential<TUserId>>>GetByUserAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ICredential<TUserId>>>GetByUserAndTypeAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default);
    Task AddAsync(TenantKey tenant, ICredential<TUserId> credential, CancellationToken ct = default);
    Task UpdateSecurityStateAsync(TenantKey tenant, TUserId userId, CredentialType type, CredentialSecurityState securityState, CancellationToken ct = default);
    Task UpdateMetadataAsync(TenantKey tenant, TUserId userId, CredentialType type, CredentialMetadata metadata, CancellationToken ct = default);
    Task DeleteAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default);
    Task DeleteByUserAsync(TenantKey tenant, TUserId userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(TenantKey tenant, TUserId userId, CredentialType type, CancellationToken ct = default);
}
