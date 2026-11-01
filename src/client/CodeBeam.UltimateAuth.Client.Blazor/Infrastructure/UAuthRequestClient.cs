using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Net;
using System.Text.Json;

// TODO: Add fluent helper API like RequiredOk
namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthRequestClient : IUAuthRequestClient
{
    private readonly IJSRuntime _js;
    IUAuthClientBootstrapper _bootstrapper;
    private UAuthClientOptions _options;

    public UAuthRequestClient(IJSRuntime js, IUAuthClientBootstrapper bootstrapper, IOptions<UAuthClientOptions> options)
    {
        _js = js;
        _bootstrapper = bootstrapper;
        _options = options.Value;
    }

    public async Task NavigateAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        await _js.InvokeVoidAsync("uauth.post", ct, new
        {
            url = endpoint,
            mode = "navigate",
            data = form,
            clientProfile = _options.ClientProfile.ToString(),
            trustedOrigin = ResolveTrustedOrigin()
        });
    }

    public async Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var result = await _js.InvokeAsync<UAuthTransportResult>("uauth.post", ct, new
        {
            url = endpoint,
            mode = "fetch",
            data = form,
            clientProfile = _options.ClientProfile.ToString(),
            trustedOrigin = ResolveTrustedOrigin()
        });

        if (result == null)
            throw new UAuthProtocolException("Invalid error response format.");

        if (result.Status == 0)
            throw new UAuthTransportException("Network error.");

        //if (result.Status >= 500)
        //    throw new UAuthTransportException($"Server error {result.Status}", (HttpStatusCode)result.Status);

        return result;
    }

    public async Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form, bool navigateOnSuccess, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var result = await _js.InvokeAsync<UAuthTransportResult>("uauth.post", ct,
            new
            {
                url = endpoint,
                mode = navigateOnSuccess ? "logout" : "logout-observe",
                data = form,
                clientProfile = _options.ClientProfile.ToString(),
                trustedOrigin = ResolveTrustedOrigin()
            });

        if (result is null)
            throw new UAuthProtocolException("Invalid logout transport response.");

        if (result.Status == 0)
            throw new UAuthTransportException("Could not confirm server logout.");

        return result;
    }

    public async Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = default, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var result = await _js.InvokeAsync<UAuthTransportResult>("uauth.postJson", ct, new
        {
            url = endpoint,
            payload = payload,
            clientProfile = _options.ClientProfile.ToString(),
            trustedOrigin = ResolveTrustedOrigin()
        });

        if (result == null)
            throw new UAuthProtocolException("Invalid error response format.");

        if (result.Status == 0)
            throw new UAuthTransportException("Network error.");

        return result;
    }

    public async Task<TTryResult> TryAndCommitAsync<TTryResult>(string tryEndpoint, string commitEndpoint, object request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var response = await _js.InvokeAsync<JsonElement>(
            "uauth.tryAndCommit",
            ct,
            new
            {
                tryUrl = tryEndpoint,
                commitUrl = commitEndpoint,
                data = request,
                clientProfile = _options.ClientProfile.ToString(),
                trustedOrigin = ResolveTrustedOrigin()
            });

        if (response.ValueKind != JsonValueKind.Object)
            throw new UAuthProtocolException("Invalid tryAndCommit response.");
        if (response.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var code) && code == 429)
            throw UAuthRateLimit.Exception(response.TryGetProperty("retryAfter", out var retry) && retry.ValueKind == JsonValueKind.String ? retry.GetString() : null);
        try
        {
            return response.Deserialize<TTryResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new UAuthProtocolException("Invalid tryAndCommit response.");
        }
        catch (JsonException ex) { throw new UAuthProtocolException("Invalid tryAndCommit response.", ex); }
    }

    private string? ResolveTrustedOrigin()
    {
        var basePath = _options.Endpoints.BasePath;

        if (Uri.TryCreate(basePath, UriKind.Absolute, out var uri))
            return uri.GetLeftPart(UriPartial.Authority);

        // Relative BasePath: JS automatically uses the current origin.
        return null;
    }
}
