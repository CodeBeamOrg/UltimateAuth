using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class LoginRedirectOptions
{
    public bool RedirectEnabled { get; set; } = true;

    public string SuccessRedirect { get; set; } = "/";
    public string FailureRedirect { get; set; } = "/login";

    /// <summary>
    /// Whether query-based returnUrl override is allowed.
    /// </summary>
    public bool AllowReturnUrlOverride { get; set; } = true;

    internal LoginRedirectOptions Clone() => new()
    {
        RedirectEnabled = RedirectEnabled,
        SuccessRedirect = SuccessRedirect,
        FailureRedirect = FailureRedirect,
        AllowReturnUrlOverride = AllowReturnUrlOverride,
    };
}
