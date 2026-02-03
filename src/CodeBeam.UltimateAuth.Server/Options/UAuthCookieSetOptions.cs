using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCookieSetOptions
{
    public bool EnableSessionCookie { get; set; } = true;
    public bool EnableAccessTokenCookie { get; set; } = true;
    public bool EnableRefreshTokenCookie { get; set; } = true;

    public UAuthCookieOptions Session { get; init; } = new()
    {
        Name = "uas",
        HttpOnly = true,
        SameSite = SameSiteMode.None
    };

    public UAuthCookieOptions RefreshToken { get; init; } = new()
    {
        Name = "uar",
        HttpOnly = true,
        SameSite = SameSiteMode.None
    };

    public UAuthCookieOptions AccessToken { get; init; } = new()
    {
        Name = "uat",
        HttpOnly = true,
        SameSite = SameSiteMode.None
    };

    internal UAuthCookieSetOptions Clone() => new()
    {
        Session = Session.Clone(),
        RefreshToken = RefreshToken.Clone(),
        AccessToken = AccessToken.Clone()
    };

}
