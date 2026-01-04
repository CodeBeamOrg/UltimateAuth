using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthCookieSetOptions
    {
        public UAuthCookieOptions Session { get; init; } = new()
        {
            Name = "uas",
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        };

        public UAuthCookieOptions RefreshToken { get; init; } = new()
        {
            Name = "uar",
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        };

        public UAuthCookieOptions AccessToken { get; init; } = new()
        {
            Name = "uat",
            HttpOnly = false,
            SameSite = SameSiteMode.None
        };

        internal UAuthCookieSetOptions Clone() => new()
        {
            Session = Session.Clone(),
            RefreshToken = RefreshToken.Clone(),
            AccessToken = AccessToken.Clone()
        };

    }
}
