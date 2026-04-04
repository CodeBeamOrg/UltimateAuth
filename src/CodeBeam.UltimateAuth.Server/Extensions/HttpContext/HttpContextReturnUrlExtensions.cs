using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class HttpContextReturnUrlExtensions
{
    public static async Task<string?> GetReturnUrlAsync(this HttpContext ctx)
    {
        if (ctx.Request.Query.TryGetValue(UAuthConstants.Query.ReturnUrl, out var query))
        {
            return query.ToString();
        }

        if (ctx.Request.HasFormContentType)
        {
            try
            {
                var form = await ctx.GetCachedFormAsync();

                if (form?.TryGetValue(UAuthConstants.Form.ReturnUrl, out var formValue) == true)
                {
                    return formValue.ToString();
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        return null;
    }
}
