using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveRedirectResponse
{
    public bool Enabled { get; }
    public string? SuccessPath { get; }
    public string? FailurePath { get; }
    public IReadOnlyDictionary<AuthFailureReason, string>? FailureCodes { get; }
    public bool AllowReturnUrlOverride { get; }
    public bool IncludeLockoutTiming { get; }
    public bool IncludeRemainingAttempts { get; }

    public EffectiveRedirectResponse(
        bool enabled,
        string? successPath,
        string? failurePath,
        IReadOnlyDictionary<AuthFailureReason, string>? failureCodes,
        bool allowReturnUrlOverride,
        bool includeLockoutTiming,
        bool includeRemainingAttempts)
    {
        Enabled = enabled;
        SuccessPath = successPath;
        FailurePath = failurePath;
        FailureCodes = failureCodes;
        AllowReturnUrlOverride = allowReturnUrlOverride;
        IncludeLockoutTiming = includeLockoutTiming;
        IncludeRemainingAttempts = includeRemainingAttempts;
    }

    public static readonly EffectiveRedirectResponse Disabled = new(false, null, null, null, false, false, false);

    public static EffectiveRedirectResponse FromLogin(LoginRedirectOptions login)
    => new(
        login.RedirectEnabled,
        login.SuccessRedirect,
        login.FailureRedirect,
        login.FailureCodes,
        login.AllowReturnUrlOverride,
        login.IncludeLockoutTiming,
        login.IncludeRemainingAttempts
    );

    public static EffectiveRedirectResponse FromLogout(LogoutRedirectOptions logout)
        => new(
            logout.RedirectEnabled,
            logout.RedirectUrl,
            null,
            null,
            logout.AllowReturnUrlOverride,
            false,
            false
        );
}
