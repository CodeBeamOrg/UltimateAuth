using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class LoginRedirectOptions
{
    public bool RedirectEnabled { get; set; } = true;

    public string SuccessRedirect { get; set; } = "/";
    public string FailureRedirect { get; set; } = "/login";

    public string FailureQueryKey { get; set; } = "error";
    public string CodeQueryKey { get; set; } = "code";

    public Dictionary<AuthFailureReason, string> FailureCodes { get; set; } = new();

    /// <summary>
    /// Whether query-based returnUrl override is allowed.
    /// </summary>
    public bool AllowReturnUrlOverride { get; set; } = true;

    internal LoginRedirectOptions Clone() => new()
    {
        RedirectEnabled = RedirectEnabled,
        SuccessRedirect = SuccessRedirect,
        FailureRedirect = FailureRedirect,
        FailureQueryKey = FailureQueryKey,
        CodeQueryKey = CodeQueryKey,
        FailureCodes = new Dictionary<AuthFailureReason, string>(FailureCodes),
        AllowReturnUrlOverride = AllowReturnUrlOverride
    };
}
