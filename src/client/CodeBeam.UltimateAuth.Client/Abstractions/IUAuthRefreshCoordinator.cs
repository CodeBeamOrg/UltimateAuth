namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IUAuthRefreshCoordinator
{
    Task<RefreshResult> RefreshAsync(UAuthRefreshTrigger trigger, CancellationToken ct = default);
    Task<RefreshResult> RefreshIfCurrentAsync(UAuthRefreshTrigger trigger, UAuthTokenSnapshot expected, CancellationToken ct = default);
}
