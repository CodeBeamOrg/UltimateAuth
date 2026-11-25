using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveRedirectResponse
{
    public bool Enabled { get; }
    public string? SuccessPath { get; }
    public string? FailurePath { get; }
    public bool AllowReturnUrlOverride { get; }

    public EffectiveRedirectResponse(
        bool enabled,
        string? successPath,
        string? failurePath,
        bool allowReturnUrlOverride)
    {
        Enabled = enabled;
        SuccessPath = successPath;
        FailurePath = failurePath;
        AllowReturnUrlOverride = allowReturnUrlOverride;
    }

    public static readonly EffectiveRedirectResponse Disabled = new(false, null, null, false);

    public static EffectiveRedirectResponse FromLogin(LoginRedirectOptions login)
    => new(
        login.RedirectEnabled,
        login.SuccessRedirect,
        login.FailureRedirect,
        login.AllowReturnUrlOverride
    );

    public static EffectiveRedirectResponse FromLogout(LogoutRedirectOptions logout)
        => new(
            logout.RedirectEnabled,
            logout.RedirectUrl,
            null,
            logout.AllowReturnUrlOverride
        );
}
