using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthResourceRequestClient : IUAuthResourceRequestClient
{
    private readonly IJSRuntime _js;
    private readonly IUAuthClientBootstrapper _bootstrapper;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly IUAuthRequestRecovery _recovery;
    private readonly UAuthClientOptions _options;

    public UAuthResourceRequestClient(IJSRuntime js, IUAuthClientBootstrapper bootstrapper, IUAuthTokenStore tokenStore, IUAuthRequestRecovery recovery, IOptions<UAuthClientOptions> options)
    {
        _js = js;
        _bootstrapper = bootstrapper;
        _tokenStore = tokenStore;
        _recovery = recovery;
        _options = options.Value;
    }

    public async Task<UAuthResourceTransportResult> SendAsync(string method, string endpoint, object? payload = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var snapshot = await _recovery.PrepareAsync(ct);

        var first = await SendRawAsync(method, endpoint, payload, snapshot, ct);

        if (first.Status != 401 || !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return first;
        }

        var refreshed = await _recovery.RecoverAsync(snapshot, ct);

        if (refreshed is null)
            return first;

        return await SendRawAsync(method, endpoint, payload, refreshed, ct);
    }

    private async Task<UAuthResourceTransportResult> SendRawAsync(string method, string endpoint, object? payload, UAuthTokenSnapshot snapshot, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync(ct);

        ct.ThrowIfCancellationRequested();

        var requestId = Guid.NewGuid().ToString("N");

        using var registration = ct.Register(() =>
        {
            _ = CancelRequestAsync(requestId);
        });

        var token = snapshot.Token;

        if (token is not null && token.IsExpired(DateTimeOffset.UtcNow))
        {
            token = null;
        }

        var result = await _js.InvokeAsync<UAuthResourceTransportResult>(
            "uauth.resourceRequest",
            ct,
            new
            {
                url = endpoint,
                baseAddress = _options.Resource.BaseAddress,
                method,
                payload,
                applicationProfile = _options.ApplicationProfile.ToString(),
                credentials = _options.Resource.Credentials.ToString(),
                accessToken = token?.Token,
                requestId
            });

        if (result is null)
        {
            throw new UAuthProtocolException("Invalid Resource API transport response.");
        }

        if (result.Status == 0)
        {
            throw new UAuthTransportException("Resource API network error.");
        }

        return result;
    }

    private async Task CancelRequestAsync(string requestId)
    {
        try
        {
            await _js.InvokeVoidAsync("uauth.cancelResourceRequest", requestId);
        }
        catch (JSDisconnectedException)
        {
            // Browser circuit is no longer available.
        }
        catch (JSException)
        {
            // Cancellation is best-effort.
        }
    }
}
