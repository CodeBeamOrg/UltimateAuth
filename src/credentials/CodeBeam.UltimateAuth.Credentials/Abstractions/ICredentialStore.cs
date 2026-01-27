using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials
{
    public interface ICredentialStore<TUserId>
    {
        Task<IReadOnlyCollection<ICredential<TUserId>>>FindByLoginAsync(string? tenantId, string loginIdentifier, CancellationToken ct = default);
        Task<IReadOnlyCollection<ICredential<TUserId>>>GetByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
        Task<IReadOnlyCollection<ICredential<TUserId>>>GetByUserAndTypeAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default);
        Task AddAsync(string? tenantId, ICredential<TUserId> credential, CancellationToken ct = default);
        Task UpdateSecurityStateAsync(string? tenantId, TUserId userId, CredentialType type, CredentialSecurityState securityState, CancellationToken ct = default);
        Task UpdateMetadataAsync(string? tenantId, TUserId userId, CredentialType type, CredentialMetadata metadata, CancellationToken ct = default);
        Task DeleteAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default);
        Task DeleteByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string? tenantId, TUserId userId, CredentialType type, CancellationToken ct = default);
    }
}
