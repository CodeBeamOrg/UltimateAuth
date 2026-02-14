namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Orchestrates the lifecycle of UAuthState.
/// This is the single authority responsible for keeping
/// client-side authentication state in sync with the server.
/// </summary>
public interface IUAuthStateManager
{
    /// <summary>
    /// Current in-memory authentication state.
    /// </summary>
    UAuthState State { get; }

    /// <summary>
    /// Ensures the authentication state is valid.
    /// May call server validate/refresh if needed.
    /// </summary>
    Task EnsureAsync(bool force = false, CancellationToken ct = default);

    /// <summary>
    /// Called after a successful login.
    /// </summary>
    Task OnLoginAsync();

    /// <summary>
    /// Called after logout.
    /// </summary>
    Task OnLogoutAsync();

    /// <summary>
    /// Forces state to be cleared and re-validation required.
    /// </summary>
    void MarkStale();
}
