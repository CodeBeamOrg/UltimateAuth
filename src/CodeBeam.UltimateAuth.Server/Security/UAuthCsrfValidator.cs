using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class UAuthCsrfValidator(IAntiforgery antiforgery)
{
    public async Task<bool> ValidateAsync(HttpContext context)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
