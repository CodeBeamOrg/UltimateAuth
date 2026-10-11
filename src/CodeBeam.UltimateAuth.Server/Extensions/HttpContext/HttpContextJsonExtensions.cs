using System.Text.Json;
using CodeBeam.UltimateAuth.Core.Errors;
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
            throw new UAuthValidationException("request_content_type_invalid");

        if (request.Body == null || request.ContentLength == 0)
            throw new UAuthValidationException("request_body_required");

        request.EnableBuffering();

        request.Body.Position = 0;

        try
        {
            var result = await JsonSerializer.DeserializeAsync<T>(request.Body, JsonOptions, ct);

            if (result == null)
                throw new UAuthValidationException("request_body_invalid");

            return result;
        }
        catch (JsonException)
        {
            throw new UAuthValidationException("request_json_invalid");
        }
        finally
        {
            if (request.Body.CanSeek)
                request.Body.Position = 0;
        }
    }
}
