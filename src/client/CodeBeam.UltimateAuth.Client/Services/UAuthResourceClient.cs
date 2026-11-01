using System.Text.Json;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthResourceClient : IResourceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUAuthResourceRequestClient _transport;

    public UAuthResourceClient(IUAuthResourceRequestClient transport)
    {
        _transport = transport;
    }

    public Task<UAuthResult<T>> GetAsync<T>(string endpoint, CancellationToken ct = default)
        => SendAsync<T>("GET", endpoint, null, ct);

    public Task<UAuthResult<T>> PostAsync<T>(string endpoint, object? request, CancellationToken ct = default)
        => SendAsync<T>("POST", endpoint, request, ct);

    public Task<UAuthResult<T>> PutAsync<T>(string endpoint, object? request, CancellationToken ct = default)
        => SendAsync<T>("PUT", endpoint, request, ct);

    public Task<UAuthResult<T>> PatchAsync<T>(string endpoint, object? request, CancellationToken ct = default)
        => SendAsync<T>("PATCH", endpoint, request, ct);

    public async Task<UAuthResult> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await _transport.SendAsync("DELETE", endpoint, null, ct);

        return new UAuthResult
        {
            IsSuccess = (response.Ok && response.Status is >= 200 and < 300),
            Status = response.Status,
            Problem = (response.Ok && response.Status is >= 200 and < 300) ? null : ReadProblem(response)
        };
    }

    private async Task<UAuthResult<T>> SendAsync<T>(string method, string endpoint, object? request, CancellationToken ct)
    {
        var response = await _transport.SendAsync(method, endpoint, request, ct);

        return new UAuthResult<T>
        {
            IsSuccess = (response.Ok && response.Status is >= 200 and < 300),
            Status = response.Status,
            Value = (response.Ok && response.Status is >= 200 and < 300) ? ReadValue<T>(response) : default,
            Problem = (response.Ok && response.Status is >= 200 and < 300) ? null : ReadProblem(response)
        };
    }

    private static T? ReadValue<T>(UAuthResourceTransportResult response)
    {
        if (string.IsNullOrWhiteSpace(response.Body))
            return default;

        return JsonSerializer.Deserialize<T>(response.Body, JsonOptions);
    }

    private static UAuthProblem? ReadProblem(UAuthResourceTransportResult response)
    {
        if (response.Status == 429) return UAuthRateLimit.Problem(response.RetryAfter);
        if (string.IsNullOrWhiteSpace(response.Body))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UAuthProblem>(response.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
