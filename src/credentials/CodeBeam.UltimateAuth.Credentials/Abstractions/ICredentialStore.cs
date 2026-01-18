namespace CodeBeam.UltimateAuth.Credentials
{
    public interface ICredentialStore<TUserId>
    {
        Task<ICredential<TUserId>?> FindByLoginAsync(string? tenantId, string loginIdentifier, CancellationToken ct = default);
        Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
        Task DeleteByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
    }
}
