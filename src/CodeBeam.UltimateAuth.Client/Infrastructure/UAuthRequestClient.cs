using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

// TODO: Add fluent helper API like RequiredOk
namespace CodeBeam.UltimateAuth.Client.Infrastructure;

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
            clientProfile = _options.ClientProfile.ToString()
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
            expectJson = false,
            data = form,
            clientProfile = _options.ClientProfile.ToString()
        });

        return result;
    }

    public async Task<UAuthTransportResult> SendFormForJsonAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        var postData = form ?? new Dictionary<string, string>();
        return await _js.InvokeAsync<UAuthTransportResult>("uauth.post", ct,
            new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = true,
                data = postData,
                clientProfile = _options.ClientProfile.ToString()
            });
    }

    public async Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = default, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _bootstrapper.EnsureStartedAsync();

        return await _js.InvokeAsync<UAuthTransportResult>("uauth.postJson", ct, new
        {
            url = endpoint,
            payload = payload,
            clientProfile = _options.ClientProfile.ToString()
        });
    }

}
