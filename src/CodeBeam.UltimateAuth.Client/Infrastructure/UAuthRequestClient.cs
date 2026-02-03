using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

// TODO: Add fluent helper API like RequiredOk
namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthRequestClient : IUAuthRequestClient
{
    private readonly IJSRuntime _js;
    private UAuthOptions _coreOptions;

    public UAuthRequestClient(IJSRuntime js, IOptions<UAuthOptions> coreOptions)
    {
        _js = js;
        _coreOptions = coreOptions.Value;
    }

    public Task NavigateAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return _js.InvokeVoidAsync("uauth.post", ct, new
        {
            url = endpoint,
            mode = "navigate",
            data = form,
            clientProfile = _coreOptions.ClientProfile.ToString()
        }).AsTask();
    }

    public async Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = await _js.InvokeAsync<UAuthTransportResult>("uauth.post", ct, new
        {
            url = endpoint,
            mode = "fetch",
            expectJson = false,
            data = form,
            clientProfile = _coreOptions.ClientProfile.ToString()
        });

        return result;
    }

    public async Task<UAuthTransportResult> SendFormForJsonAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var postData = form ?? new Dictionary<string, string>();
        return await _js.InvokeAsync<UAuthTransportResult>("uauth.post", ct,
            new
            {
                url = endpoint,
                mode = "fetch",
                expectJson = true,
                data = postData,
                clientProfile = _coreOptions.ClientProfile.ToString()
            });
    }

    public async Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = default, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return await _js.InvokeAsync<UAuthTransportResult>("uauth.postJson", ct, new
        {
            url = endpoint,
            payload = payload,
            clientProfile = _coreOptions.ClientProfile.ToString()
        });
    }

}
