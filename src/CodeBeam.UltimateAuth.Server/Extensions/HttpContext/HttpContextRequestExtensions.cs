using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

internal static class HttpContextRequestExtensions
{
    private const string FormCacheKey = "__uauth_form";

    public static async Task<IFormCollection?> GetCachedFormAsync(this HttpContext ctx)
    {
        if (!ctx.Request.HasFormContentType)
            return null;

        if (ctx.Items.TryGetValue(FormCacheKey, out var existing) && existing is IFormCollection cached)
            return cached;

        try
        {
            ctx.Request.EnableBuffering();
            var form = await ctx.Request.ReadFormAsync();
            ctx.Request.Body.Position = 0;
            ctx.Items[FormCacheKey] = form;
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
