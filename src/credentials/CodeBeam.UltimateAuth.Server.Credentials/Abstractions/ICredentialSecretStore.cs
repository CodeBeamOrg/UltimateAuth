namespace CodeBeam.UltimateAuth.Server.Credentials
{
    public interface ICredentialSecretStore<TUserId>
    {
        Task UpdateSecretAsync(string? tenantId, TUserId userId, CredentialType type, string newSecretHash, CancellationToken ct = default);
    }
}
