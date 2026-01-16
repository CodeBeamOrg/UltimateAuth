namespace CodeBeam.UltimateAuth.Users
{
    public interface IUserSecurityStateProvider<TUserId>
    {
        Task<IUserSecurityState?> GetAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
    }
}
