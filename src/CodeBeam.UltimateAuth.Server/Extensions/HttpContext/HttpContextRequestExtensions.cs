using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class HttpContextRequestExtensions
{
    public static async Task<IFormCollection?> GetCachedFormAsync(this HttpContext ctx)
    {
        if (!ctx.Request.HasFormContentType)
            return null;

        if (ctx.Items.TryGetValue(UAuthConstants.Form.FormCacheKey, out var existing) && existing is IFormCollection cached)
            return cached;

        try
        {
            ctx.Request.EnableBuffering();
            var form = await ctx.Request.ReadFormAsync();
            ctx.Request.Body.Position = 0;
            ctx.Items[UAuthConstants.Form.FormCacheKey] = form;
            return form;
        }
        catch (IOException)
        {
            return null;
        }
        catch
        {
            throw new InvalidOperationException();
        }
    }
}
