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

    public UAuthResourceRequestClient(IJSRuntime js, IUAuthClientBootstrapper bootstrapper, IOptions<UAuthClientOptions> options)
    {
        _js = js;
        _bootstrapper = bootstrapper;
        _options = options.Value;
    }

    public async Task<UAuthResourceTransportResult> SendAsync(string method, string endpoint, object? payload = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        ct.ThrowIfCancellationRequested();

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
                    clientProfile = _options.ClientProfile.ToString(),
                    credentials = _options.Resource.Credentials.ToString(),
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
