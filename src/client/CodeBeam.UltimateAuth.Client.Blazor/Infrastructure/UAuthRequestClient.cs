using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Text.Json;

// TODO: Add fluent helper API like RequiredOk
namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthRequestClient : IUAuthRequestClient
{
    private readonly IJSRuntime _js;
    private readonly IUAuthClientBootstrapper _bootstrapper;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly UAuthClientOptions _options;
    private readonly NavigationManager _navigation;

    public UAuthRequestClient(
        IJSRuntime js,
        IUAuthClientBootstrapper bootstrapper,
        IOptions<UAuthClientOptions> options,
        IUAuthTokenStore tokenStore,
        NavigationManager navigation)
    {
        _js = js;
        _bootstrapper = bootstrapper;
        _options = options.Value;
        _tokenStore = tokenStore;
        _navigation = navigation;
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
            applicationProfile = _options.ApplicationProfile.ToString(),
            trustedOrigin = ResolveTrustedOrigin()
        });
    }

    public Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
        => SendFormAsync(endpoint, form, accessToken: null, ct);

    public async Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form, string? accessToken, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var result = await _js.InvokeAsync<UAuthTransportResult>(
            "uauth.post",
            ct,
            new
            {
                url = endpoint,
                mode = "fetch",
                data = form,
                accessToken,
                applicationProfile = _options.ApplicationProfile.ToString(),
                trustedOrigin = ResolveTrustedOrigin()
            });

        if (result is null)
            throw new UAuthProtocolException("Invalid error response format.");

        if (result.Status == 0)
            throw new UAuthTransportException("Network error.");

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
                applicationProfile = _options.ApplicationProfile.ToString(),
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

        var result = await _js.InvokeAsync<UAuthTransportResult>(
            "uauth.postJson",
            ct,
            new
            {
                url = endpoint,
                payload,
                accessToken = GetAccessToken(),
                applicationProfile = _options.ApplicationProfile.ToString(),
                trustedOrigin = ResolveTrustedOrigin()
            });

        if (result is null)
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
                applicationProfile = _options.ApplicationProfile.ToString(),
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

    public async Task<UAuthLoginTransportInfo> GetLoginTransportAsync(string endpoint, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync(ct);

        var response = await _js.InvokeAsync<UAuthTransportResult>(
            "uauth.getLoginTransport",
            ct,
            new
            {
                url = endpoint,
                applicationProfile = _options.ApplicationProfile.ToString(),
                trustedOrigin = ResolveTrustedOrigin()
            });

        if (response is null || !response.Ok || response.Body is null)
        {
            throw new UAuthProtocolException("Unable to resolve effective login transport.");
        }

        return response.Body.Value.Deserialize<UAuthLoginTransportInfo>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new UAuthProtocolException("Invalid login transport response.");
    }

    public Task NavigateToUrlAsync(string url, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Navigation URL is required.", nameof(url));

        if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith(@"\\", StringComparison.Ordinal))
        {
            throw new UAuthProtocolException("Invalid login redirect URL.");
        }

        var target = _navigation.ToAbsoluteUri(url);
        var current = new Uri(_navigation.Uri);

        if (target.Scheme != current.Scheme || target.Authority != current.Authority ||
            !string.IsNullOrEmpty(target.UserInfo))
        {
            throw new UAuthProtocolException("Fetch login navigation must remain on the application origin.");
        }

        // Keep the Blazor Server circuit and its scoped token store.
        _navigation.NavigateTo(target.ToString(), forceLoad: false);

        return Task.CompletedTask;
    }


    private string? GetAccessToken()
    {
        var token = _tokenStore.Read();

        if (token is null)
            return null;

        if (token.IsExpired(DateTimeOffset.UtcNow))
        {
            _tokenStore.Clear();
            return null;
        }

        return token.Token;
    }

    private string? ResolveTrustedOrigin()
    {
        var basePath = _options.Endpoints.BasePath;

        if (Uri.TryCreate(basePath, UriKind.Absolute, out var hubUri) &&
            (hubUri.Scheme == Uri.UriSchemeHttp || hubUri.Scheme == Uri.UriSchemeHttps))
        {
            return hubUri.GetLeftPart(UriPartial.Authority);
        }

        return null;
    }
}
