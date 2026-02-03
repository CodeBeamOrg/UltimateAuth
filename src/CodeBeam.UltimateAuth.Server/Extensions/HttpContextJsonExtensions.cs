using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class HttpContextJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadJsonAsync<T>(this HttpContext ctx, CancellationToken ct = default)
    {
        if (!ctx.Request.HasJsonContentType())
            throw new InvalidOperationException("Request content type must be application/json.");

        if (ctx.Request.Body is null)
            throw new InvalidOperationException("Request body is empty.");

        var result = await JsonSerializer.DeserializeAsync<T>(ctx.Request.Body, JsonOptions, ct);

        if (result is null)
            throw new InvalidOperationException("Request body could not be deserialized.");

        return result;
    }
}
