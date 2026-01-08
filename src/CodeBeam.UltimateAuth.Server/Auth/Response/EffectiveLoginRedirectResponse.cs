using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public sealed record EffectiveLoginRedirectResponse
    (
        bool RedirectEnabled,
        string SuccessPath,
        string FailurePath,
        string FailureQueryKey,
        string CodeQueryKey,
        IReadOnlyDictionary<AuthFailureReason, string> FailureCodes
    );
}
