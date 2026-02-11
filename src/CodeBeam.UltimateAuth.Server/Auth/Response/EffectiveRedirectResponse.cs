using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveRedirectResponse
{
    public bool Enabled { get; }
    public string? SuccessPath { get; }
    public string? FailurePath { get; }
    public string? FailureQueryKey { get; }
    public IReadOnlyDictionary<AuthFailureReason, string>? FailureCodes { get; }
    public bool AllowReturnUrlOverride { get; }

    public EffectiveRedirectResponse(
        bool enabled,
        string? successPath,
        string? failurePath,
        string? failureQueryKey,
        IReadOnlyDictionary<AuthFailureReason, string>? failureCodes,
        bool allowReturnUrlOverride)
    {
        Enabled = enabled;
        SuccessPath = successPath;
        FailurePath = failurePath;
        FailureQueryKey = failureQueryKey;
        FailureCodes = failureCodes;
        AllowReturnUrlOverride = allowReturnUrlOverride;
    }

    public static readonly EffectiveRedirectResponse Disabled = new(false, null, null, null, null, false);

    public static EffectiveRedirectResponse FromLogin(LoginRedirectOptions login)
        => new(
            login.RedirectEnabled,
            login.SuccessRedirect,
            login.FailureRedirect,
            login.FailureQueryKey,
            login.FailureCodes,
            login.AllowReturnUrlOverride
        );

    public static EffectiveRedirectResponse FromLogout(LogoutRedirectOptions logout)
        => new(
            logout.RedirectEnabled,
            logout.RedirectUrl,
            null,
            null,
            null,
            logout.AllowReturnUrlOverride
        );
}
