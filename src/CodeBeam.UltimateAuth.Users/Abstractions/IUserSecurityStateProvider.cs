namespace CodeBeam.UltimateAuth.Server.Users
{
    public interface IUserSecurityStateProvider<TUserId>
    {
        Task<IUserSecurityState?> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
    }
}
