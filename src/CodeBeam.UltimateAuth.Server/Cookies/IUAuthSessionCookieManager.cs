using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

/// <summary>
/// Responsible for issuing, reading and revoking
/// UltimateAuth session cookies.
/// </summary>
public interface IUAuthSessionCookieManager
{
    void Issue(HttpContext context, string sessionId);
    bool TryRead(HttpContext context, out string sessionId);
    void Revoke(HttpContext context);
}
