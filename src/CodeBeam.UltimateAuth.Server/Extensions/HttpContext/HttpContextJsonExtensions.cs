using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class HttpContextJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> ReadJsonAsync<T>(this HttpContext ctx, CancellationToken ct = default)
    {
        var request = ctx.Request;

        if (!request.HasJsonContentType())
            throw new InvalidOperationException("Request content type must be application/json.");

        if (request.Body == null || request.ContentLength == 0)
            throw new InvalidOperationException("Request body is empty.");

        request.EnableBuffering();

        request.Body.Position = 0;

        try
        {
            var result = await JsonSerializer.DeserializeAsync<T>(request.Body, JsonOptions, ct);

            request.Body.Position = 0;

            if (result == null)
                throw new InvalidOperationException("Request body could not be deserialized.");

            return result;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Invalid JSON");
        }
    }
}
