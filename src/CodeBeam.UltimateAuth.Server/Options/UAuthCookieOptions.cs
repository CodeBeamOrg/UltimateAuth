using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCookieOptions
{
    public string Name { get; set; } = default!;

    public bool HttpOnly { get; set; } = true; //  TODO: Add UAUTH002 diagnostic if false?

    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    public SameSiteMode? SameSite { get; set; }

    public string Path { get; set; } = "/";

    /// <summary>
    /// If set, defines absolute expiration for the cookie.
    /// If null, a session cookie is used.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    public UAuthCookieLifetimeOptions Lifetime { get; set; } = new();

    internal UAuthCookieOptions Clone() => new()
    {
        Name = Name,
        HttpOnly = HttpOnly,
        SecurePolicy = SecurePolicy,
        SameSite = SameSite,
        Path = Path,
        MaxAge = MaxAge,
        Lifetime = Lifetime.Clone()
    };
}
