using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Cookies;

public interface IUAuthCookieManager
{
    void Write(HttpContext context, string value, Action<CookieOptions>? configure = null);
    
    bool TryRead(HttpContext context, out string value);

    void Delete(HttpContext context);
}
