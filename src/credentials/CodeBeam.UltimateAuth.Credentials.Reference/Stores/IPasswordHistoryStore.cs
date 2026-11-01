using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IPasswordHistoryStore
{
    Task<IReadOnlyList<PasswordHistoryEntry>> GetRecentAsync(CredentialKey credentialKey, int count, CancellationToken ct = default);

    Task AddAsync(PasswordHistoryEntry entry, CancellationToken ct = default);

    Task PruneAsync(CredentialKey credentialKey, int keepLast, CancellationToken ct = default);

    Task DeleteByCredentialAsync(CredentialKey credentialKey, CancellationToken ct = default);
    Task DeleteByUserAsync(UserKey userKey, CancellationToken ct = default);
}
