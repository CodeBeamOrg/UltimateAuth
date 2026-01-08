using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Options
{
    public sealed class UAuthClientOptions
    {
        public AuthEndpointOptions Endpoints { get; set; } = new();
        public UAuthClientRefreshOptions Refresh { get; set; } = new();
        public ReauthOptions Reauth { get; init; } = new();
    }

    public sealed class AuthEndpointOptions
    {
        /// <summary>
        /// Base URL of UAuthHub (e.g. https://localhost:6110)
        /// </summary>
        public string Authority { get; set; } = string.Empty;

        public string Login { get; set; } = "/auth/login";
        public string Logout { get; set; } = "/auth/logout";
        public string Refresh { get; set; } = "/auth/refresh";
        public string Reauth { get; set; } = "/auth/reauth";
        public string Validate { get; set; } = "/auth/validate";
    }

    public sealed class UAuthClientRefreshOptions
    {
        /// <summary>
        /// Enables background refresh coordination.
        /// Default: true for BlazorServer, false otherwise.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Interval for background refresh attempts.
        /// This is a UX / keep-alive setting, NOT a security policy.
        /// </summary>
        public TimeSpan? Interval { get; set; }

        /// <summary>
        /// Optional jitter to avoid synchronized refresh storms.
        /// </summary>
        public TimeSpan? Jitter { get; set; }
    }

    // TODO: Add ClearCookieOnReauth
    public sealed class ReauthOptions
    {
        public ReauthBehavior Behavior { get; set; } = ReauthBehavior.RedirectToLogin;
        public string LoginPath { get; set; } = "/login";
    }
}
