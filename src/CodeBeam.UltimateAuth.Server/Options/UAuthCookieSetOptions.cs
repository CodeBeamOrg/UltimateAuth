namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCookieSetOptions
{
    public UAuthCookieOptions Session { get; init; } = new()
    {
        Name = "uas",
        HttpOnly = true,
    };

    public UAuthCookieOptions RefreshToken { get; init; } = new()
    {
        Name = "uar",
        HttpOnly = true,
    };

    public UAuthCookieOptions AccessToken { get; init; } = new()
    {
        Name = "uat",
        HttpOnly = true,
    };

    internal UAuthCookieSetOptions Clone() => new()
    {
        Session = Session.Clone(),
        RefreshToken = RefreshToken.Clone(),
        AccessToken = AccessToken.Clone()
    };
}
