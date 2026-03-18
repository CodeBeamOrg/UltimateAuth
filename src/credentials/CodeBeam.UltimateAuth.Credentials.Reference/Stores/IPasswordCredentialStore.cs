using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordCredentialStore : IVersionedStore<PasswordCredential, CredentialKey>
{
    Task<IReadOnlyCollection<PasswordCredential>> GetByUserAsync(UserKey userKey, CancellationToken ct = default);
    Task DeleteByUserAsync(UserKey userKey, DeleteMode mode, DateTimeOffset now, CancellationToken ct = default);
    Task RevokeAsync(CredentialKey key, DateTimeOffset revokedAt, long expectedVersion, CancellationToken ct = default);
}
