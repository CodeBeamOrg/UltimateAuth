namespace CodeBeam.UltimateAuth.Credentials
{
    public interface ICredentialSecretStore<TUserId>
    {
        Task UpdateSecretAsync(string? tenantId, TUserId userId, CredentialType type, string newSecretHash, CancellationToken ct = default);
    }
}
