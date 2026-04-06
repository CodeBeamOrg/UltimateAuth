using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

// TODO: Add ReauthAsync
namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides authentication flow operations such as login, logout, session validation,
/// refresh, and PKCE-based authentication.
/// </summary>
/// <remarks>
/// <para>
/// This client is responsible for managing the full authentication lifecycle.
/// It abstracts different auth modes (e.g., session-based, token-based, PKCE).
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>Login and logout flows</description></item>
/// <item><description>Session validation and refresh</description></item>
/// <item><description>PKCE-based authentication (for WASM and public clients)</description></item>
/// <item><description>Device and session revocation</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Behavior depends on client profile (e.g., Blazor Server vs WASM).</description></item>
/// <item><description>Login may result in redirects or cookie/token updates.</description></item>
/// <item><description>Refresh behavior differs between PureOpaque and Hybrid modes.</description></item>
/// <item><description>Session state is managed server-side and may expire independently.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IFlowClient
{
    /// <summary>
    /// Performs a login operation.
    /// </summary>
    /// <remarks>
    /// This method triggers redirects depending on the client profile and configuration.
    /// </remarks>
    Task LoginAsync(LoginRequest request, string? returnUrl = null);

    /// <summary>
    /// Attempts to log in and returns a structured result instead of throwing. UltimateAuth suggestion as better UX.
    /// </summary>
    /// <remarks>
    /// Redirects only on successful login if mode is TryAndCommit. In TryOnly mode, it returns the result without redirecting.
    /// DirectCommit mode behaves same as LoginAsync.
    /// </remarks>
    Task<TryLoginResult> TryLoginAsync(LoginRequest request, UAuthSubmitMode mode, string? returnUrl = null);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <remarks>
    /// This clears the current session and may trigger redirects.
    /// </remarks>
    Task LogoutAsync();

    /// <summary>
    /// Refreshes the current session or tokens.
    /// </summary>
    /// <remarks>
    /// Behavior depends on auth mode:
    /// <list type="bullet">
    /// <item><description>PureOpaque: touches session</description></item>
    /// <item><description>Hybrid: rotates tokens</description></item>
    /// </list>
    /// </remarks>
    Task<RefreshResult> RefreshAsync(bool isAuto = false);

    /// <summary>
    /// Validates the current authentication state.
    /// </summary>
    /// <remarks>
    /// Can be used to check if the current session is still valid. For UI refresh, consider using IUAuthStateManager instead.
    /// </remarks>
    Task<AuthValidationResult> ValidateAsync();

    /// <summary>
    /// Starts a PKCE authentication flow and navigates to UAuthHub.
    /// </summary>
    /// <remarks>
    /// Typically used in public clients such as Blazor WASM.
    /// </remarks>
    Task BeginPkceAsync(string? returnUrl = null);

    /// <summary>
    /// Completes a PKCE login flow.
    /// </summary>
    /// <remarks>
    /// Must be called after <see cref="BeginPkceAsync"/>.
    /// </remarks>
    Task CompletePkceLoginAsync(PkceCompleteRequest request);

    /// <summary>
    /// Attempts to complete a PKCE login flow.
    /// </summary>
    /// <remarks>
    /// Redirects only on successful PKCE login if mode is TryAndCommit. In TryOnly mode, it returns the result without redirecting.
    /// DirectCommit mode behaves same as CompletePkceLoginAsync.
    /// </remarks>
    Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, UAuthSubmitMode mode);

    /// <summary>
    /// Logs out the given device session of current user.
    /// </summary>
    Task<UAuthResult<RevokeResult>> LogoutMyDeviceAsync(LogoutDeviceRequest request);

    /// <summary>
    /// Logs out all other sessions except the current one of current user.
    /// </summary>
    Task<UAuthResult> LogoutMyOtherDevicesAsync();

    /// <summary>
    /// Logs out all sessions of the current user.
    /// </summary>
    Task<UAuthResult> LogoutAllMyDevicesAsync();

    /// <summary>
    /// Logs out a specific device session for a user.
    /// </summary>
    Task<UAuthResult<RevokeResult>> LogoutUserDeviceAsync(UserKey userKey, LogoutDeviceRequest request);

    /// <summary>
    /// Logs out all other sessions for a user. Only given chain remains active.
    /// </summary>
    Task<UAuthResult> LogoutUserOtherDevicesAsync(UserKey userKey, LogoutOtherDevicesRequest request);

    /// <summary>
    /// Logs out all sessions for a user.
    /// </summary>
    Task<UAuthResult> LogoutAllUserDevicesAsync(UserKey userKey);
}
