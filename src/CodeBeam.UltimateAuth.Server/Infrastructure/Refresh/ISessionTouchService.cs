using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Refreshes session lifecycle artifacts.
    /// Used by PureOpaque and Hybrid modes.
    /// </summary>
    public interface ISessionTouchService<TUserId> : IRefreshService where TUserId : notnull
    {
        Task<SessionRefreshResult> RefreshAsync(SessionValidationResult<TUserId> validation, SessionTouchPolicy policy, DateTimeOffset now, CancellationToken ct = default);
    }
}
