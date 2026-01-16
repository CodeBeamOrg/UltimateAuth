namespace CodeBeam.UltimateAuth.Server.Credentials
{
    public interface ICredentialSecurityVersionStore<TUserId>
    {
        Task<long> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default);

        Task IncrementAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
    }
}
