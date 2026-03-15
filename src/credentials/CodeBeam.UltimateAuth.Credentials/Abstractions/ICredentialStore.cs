//using CodeBeam.UltimateAuth.Core.Abstractions;
//using CodeBeam.UltimateAuth.Core.Contracts;
//using CodeBeam.UltimateAuth.Core.Domain;
//using CodeBeam.UltimateAuth.Core.MultiTenancy;
//using CodeBeam.UltimateAuth.Credentials.Contracts;

//namespace CodeBeam.UltimateAuth.Credentials;

//public interface ICredentialStore : IVersionedStore<ICredential, CredentialKey>
//{
//    Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);

//    Task<ICredential?> GetByIdAsync(CredentialKey key, CancellationToken ct = default);

//    Task RevokeAsync(CredentialKey key, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default);

//    Task DeleteByUserAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default);
//}
