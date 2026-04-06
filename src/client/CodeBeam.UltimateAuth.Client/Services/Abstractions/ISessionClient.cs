using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides session and device management operations for the current user or administrators.
/// </summary>
/// <remarks>
/// <para>
/// This client exposes the session model of UltimateAuth, which is based on:
/// <list type="bullet">
/// <item><description><b>Root</b>: Represents the user's global session authority.</description></item>
/// <item><description><b>Chain</b>: Represents a device or client context.</description></item>
/// <item><description><b>Session</b>: Represents an individual authentication instance.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>List active sessions (by device)</description></item>
/// <item><description>Inspect session details</description></item>
/// <item><description>Revoke sessions at different levels (session, chain, root)</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Revoking is different with logout. Revoke removes trust on device and new login creates a new chain instead of continue on current.</description></item>
/// <item><description>Revoking a root is the ultimate tool which should use on security-critital situations.</description></item>
/// <item><description>Session state is server-controlled and may expire independently.</description></item>
/// <item><description>Administrative methods require elevated permissions.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISessionClient
{
    /// <summary>
    /// Retrieves chains (devices) summary for the current user.
    /// </summary>
    /// <remarks>
    /// Each chain represents a device or client context.
    /// </remarks>
    Task<UAuthResult<PagedResult<SessionChainSummary>>> GetMyChainsAsync(PageRequest? request = null);

    /// <summary>
    /// Retrieves detailed information about a specific session chain.
    /// </summary>
    /// <remarks>
    /// Includes session history and device-related information.
    /// </remarks>
    Task<UAuthResult<SessionChainDetail>> GetMyChainDetailAsync(SessionChainId chainId);

    /// <summary>
    /// Revokes a specific session chain (device).
    /// </summary>
    /// <remarks>
    /// This logs out the user from the specified device.
    /// </remarks>
    Task<UAuthResult<RevokeResult>> RevokeMyChainAsync(SessionChainId chainId);

    /// <summary>
    /// Revokes all session chains except the current one.
    /// </summary>
    /// <remarks>
    /// Useful for "log out from other devices" scenarios.
    /// </remarks>
    Task<UAuthResult> RevokeMyOtherChainsAsync();

    /// <summary>
    /// Revokes all session chains for the current user.
    /// </summary>
    /// <remarks>
    /// This logs out the user from all devices with clearing all device trusts.
    /// </remarks>
    Task<UAuthResult> RevokeAllMyChainsAsync();


    /// <summary>
    /// Retrieves session chains (devices) for a specific user.
    /// </summary>
    /// <remarks>
    /// Requires administrative privileges.
    /// </remarks>
    Task<UAuthResult<PagedResult<SessionChainSummary>>> GetUserChainsAsync(UserKey userKey, PageRequest? request = null);

    /// <summary>
    /// Retrieves detailed session chain information for a specific user.
    /// </summary>
    Task<UAuthResult<SessionChainDetail>> GetUserChainDetailAsync(UserKey userKey, SessionChainId chainId);

    /// <summary>
    /// Revokes a specific session instance for a user.
    /// </summary>
    /// <remarks>
    /// This invalidates a single session without affecting the entire device (chain).
    /// </remarks>
    Task<UAuthResult> RevokeUserSessionAsync(UserKey userKey, AuthSessionId sessionId);

    /// <summary>
    /// Revokes a session chain (device) for a user.
    /// </summary>
    Task<UAuthResult<RevokeResult>> RevokeUserChainAsync(UserKey userKey, SessionChainId chainId);

    /// <summary>
    /// Revokes the root session for a user.
    /// </summary>
    /// <remarks>
    /// This invalidates all sessions and chains for the user.
    /// </remarks>
    Task<UAuthResult> RevokeUserRootAsync(UserKey userKey);

    /// <summary>
    /// Revokes all session chains (devices) for a user.
    /// </summary>
    /// <remarks>
    /// Equivalent to logging the user out from all devices.
    /// </remarks>
    Task<UAuthResult> RevokeAllUserChainsAsync(UserKey userKey);
}
