using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

public interface IUAuthCookieManager
{
    void Write(
        HttpContext ctx,
        string value,
        UAuthCookieOptions options,
        TimeSpan? logicalLifetime);

    bool TryRead(
        HttpContext ctx,
        string cookieName,
        out string value);

    void Delete(
        HttpContext ctx,
        UAuthCookieOptions options);
}
