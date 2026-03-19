using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Options;

// TODO: Add ClearCookieOnReauth
public sealed class UAuthClientReauthOptions
{
    public ReauthBehavior Behavior { get; set; } = ReauthBehavior.Redirect;
    public string? RedirectPath { get; set; }
}
