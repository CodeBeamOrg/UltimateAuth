using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class RedirectResponseOptions
    {
        public bool Enabled { get; set; } = true;

        public string SuccessRedirect { get; init; } = "/";
        public string FailureRedirect { get; init; } = "/login";

        public string FailureQueryKey { get; init; } = "error";
        public string CodeQueryKey { get; set; } = "code";

        public Dictionary<AuthFailureReason, string> FailureCodes { get; set; }
        = new()
        {
            [AuthFailureReason.InvalidCredentials] = "invalid",
            [AuthFailureReason.LockedOut] = "locked",
            [AuthFailureReason.RequiresMfa] = "mfa"
        };
    }

}
