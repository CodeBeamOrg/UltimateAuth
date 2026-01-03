using CodeBeam.UltimateAuth.Core.Options;
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

    /// <summary>
    /// Cookie path. Default is "/".
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// If set, defines absolute expiration for the cookie.
    /// If null, a session cookie is used.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Additional tolerance added to session idle timeout
    /// when resolving cookie lifetime.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan IdleBuffer { get; set; } = TimeSpan.FromMinutes(5);

    internal UAuthCookieOptions Clone() => new()
    {
        Name = Name,
        HttpOnly = HttpOnly,
        SecurePolicy = SecurePolicy,
        SameSiteOverride = SameSiteOverride,
        Path = Path,
        MaxAge = MaxAge,
        IdleBuffer = IdleBuffer
    };
}
