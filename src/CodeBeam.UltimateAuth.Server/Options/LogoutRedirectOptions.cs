namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class LogoutRedirectOptions
{
    /// <summary>
    /// Whether logout endpoint performs a redirect.
    /// </summary>
    public bool RedirectEnabled { get; set; } = true;

    /// <summary>
    /// Default redirect URL after logout.
    /// </summary>
    public string RedirectUrl { get; set; } = "/login";

    /// <summary>
    /// Whether query-based returnUrl override is allowed.
    /// </summary>
    public bool AllowReturnUrlOverride { get; set; } = true;

    internal LogoutRedirectOptions Clone() => new()
    {
        RedirectEnabled = RedirectEnabled,
        RedirectUrl = RedirectUrl,
        AllowReturnUrlOverride = AllowReturnUrlOverride
    };

}
