using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Application-level session command API.
/// Represents explicit intent to mutate session state.
/// All operations are authorization- and policy-aware.
/// </summary>
public interface IUAuthSessionManager
{
    /// <summary>
    /// Revokes a single session (logout current device).
    /// </summary>
    Task RevokeSessionAsync(AuthSessionId sessionId, CancellationToken ct = default);

    /// <summary>
    /// Revokes all sessions in a specific chain (logout a device).
    /// </summary>
    Task RevokeChainAsync(SessionChainId chainId, CancellationToken ct = default);

    /// <summary>
    /// Revokes all session chains for the current user (logout all devices).
    /// </summary>
    Task RevokeAllChainsAsync(UserKey userKey, SessionChainId? exceptChainId = null, CancellationToken ct = default);

    /// <summary>
    /// Hard revoke: revokes the entire session root (admin / security action).
    /// </summary>
    Task RevokeRootAsync(UserKey userKey, CancellationToken ct = default);
}
