using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Options;

// TODO: Add ClearCookieOnReauth
/// <summary>
/// Options for reauthentication behavior in the UAuth client.
/// </summary>
public sealed class UAuthClientReauthOptions
{
    /// <summary>
    /// Specifies the behavior to follow when reauthentication is required.
    /// </summary>
    public ReauthBehavior Behavior { get; set; } = ReauthBehavior.Redirect;

    /// <summary>
    /// Specifies the path to redirect to when reauthentication is required and the behavior is set to Redirect.
    /// </summary>
    public string? RedirectPath { get; set; }
}
