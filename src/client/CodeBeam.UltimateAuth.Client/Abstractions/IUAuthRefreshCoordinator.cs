namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IUAuthRefreshCoordinator
{
    Task<RefreshResult> RefreshAsync(UAuthRefreshTrigger trigger, CancellationToken ct = default);
}
