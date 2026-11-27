namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IUAuthRefreshExecutor
{
    Task<RefreshResult> ExecuteAsync(UAuthRefreshTrigger trigger);
}
