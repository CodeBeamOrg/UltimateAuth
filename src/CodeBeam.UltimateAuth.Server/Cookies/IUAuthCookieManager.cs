using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

public interface IUAuthCookieManager
{
    void Write(HttpContext context, string name, string value, CookieOptions options);

    bool TryRead(HttpContext context, string name, out string value);

    void Delete(HttpContext context, string name);
}
