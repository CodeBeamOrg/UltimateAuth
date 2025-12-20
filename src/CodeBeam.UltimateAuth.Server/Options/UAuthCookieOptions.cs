using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCookieOptions
{
    public string Name { get; set; } = "uas";

    /// <summary>
    /// Controls whether the cookie is inaccessible to JavaScript.
    /// Default: true (recommended).
    /// </summary>
    public bool HttpOnly { get; set; } = true; //  TODO: Add UAUTH002 diagnostic if false?

    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    internal SameSiteMode? SameSiteOverride { get; set; }
}
