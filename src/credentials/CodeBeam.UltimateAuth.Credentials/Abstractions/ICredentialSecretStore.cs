using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials
{
    public interface ICredentialSecretStore<TUserId>
    {
        Task SetAsync(string? tenantId, TUserId userId, CredentialType type, string secretHash, CancellationToken ct = default);
    }
}
