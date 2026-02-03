using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Refreshes session lifecycle artifacts.
/// Used by PureOpaque and Hybrid modes.
/// </summary>
public interface ISessionTouchService : IRefreshService
{
    Task<SessionRefreshResult> RefreshAsync(SessionValidationResult validation, SessionTouchPolicy policy, SessionTouchMode sessionTouchMode, DateTimeOffset now, CancellationToken ct = default);
}
