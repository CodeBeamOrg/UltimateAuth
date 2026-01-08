using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class LoginRedirectOptions
    {
        public bool RedirectEnabled { get; set; } = true;

        public string SuccessRedirect { get; init; } = "/";
        public string FailureRedirect { get; init; } = "/login";

        public string FailureQueryKey { get; init; } = "error";
        public string CodeQueryKey { get; set; } = "code";

        public Dictionary<AuthFailureReason, string> FailureCodes { get; set; } = new();

        internal LoginRedirectOptions Clone() => new()
        {
            RedirectEnabled = RedirectEnabled,
            SuccessRedirect = SuccessRedirect,
            FailureRedirect = FailureRedirect,
            FailureQueryKey = FailureQueryKey,
            CodeQueryKey = CodeQueryKey,
            FailureCodes = new Dictionary<AuthFailureReason, string>(FailureCodes)
        };

    }
}
