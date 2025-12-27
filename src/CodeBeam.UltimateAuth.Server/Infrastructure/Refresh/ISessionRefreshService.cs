using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Refreshes session lifecycle artifacts.
    /// Used by PureOpaque and Hybrid modes.
    /// </summary>
    public interface ISessionRefreshService<TUserId> : IRefreshService where TUserId : notnull
    {
        Task<SessionRefreshResult> RefreshAsync(SessionValidationResult<TUserId> validation, DateTimeOffset now, CancellationToken ct = default);
    }
}
