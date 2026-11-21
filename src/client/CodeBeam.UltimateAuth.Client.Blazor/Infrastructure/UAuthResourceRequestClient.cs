using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
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
    private readonly UAuthClientOptions _options;
    private readonly IUAuthTokenStore? _tokens;

    public UAuthResourceRequestClient(IJSRuntime js, IUAuthClientBootstrapper bootstrapper, IOptions<UAuthClientOptions> options, IUAuthTokenStore? tokens = null)
    {
        _js = js;
        _bootstrapper = bootstrapper;
        _options = options.Value;
        _tokens = tokens;
    }

    public async Task<UAuthResourceTransportResult> SendAsync(string method, string endpoint, object? payload = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        ct.ThrowIfCancellationRequested();

        var token = _options.UsePureJwtBrowserTokens ? _tokens?.Read() : null;
        if (_options.UsePureJwtBrowserTokens && (token is null || token.ExpiresAt <= DateTimeOffset.UtcNow))
            return new UAuthResourceTransportResult { Status = 401 };

        var requestId = Guid.NewGuid().ToString("N");

        using var registration = ct.Register(() =>
        {
            _ = CancelRequestAsync(requestId);
        });

        try
        {
            var result = await _js.InvokeAsync<UAuthResourceTransportResult>("uauth.resourceRequest", ct,
                new
                {
                    url = endpoint,
                    baseAddress = _options.Resource.BaseAddress,
                    method,
                    payload,
                    applicationProfile = _options.ApplicationProfile.ToString(),
                    credentials = token is null ? _options.Resource.Credentials.ToString() : "Omit",
                    accessToken = token?.Value,
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
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
