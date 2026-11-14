using CodeBeam.UltimateAuth.Client.Infrastructure;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

// Uses UltimateAuth's browser transport: cookies, profile, X-UDID and antiforgery.
public sealed class LabManagementClient(IUAuthRequestClient requests)
{
    public async Task<T> SendAsync<T>(string endpoint, object? payload = null, CancellationToken ct = default)
    {
        var result = await requests.SendJsonAsync(endpoint, payload, ct);
        if (result.Status is 401 or 403)
            throw new UnauthorizedAccessException("Management permission required.");
        if (!result.Ok || result.Body is not { } body)
            throw new InvalidOperationException("Unable to complete the operation.");
        return body.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Invalid management response.");
    }
}
