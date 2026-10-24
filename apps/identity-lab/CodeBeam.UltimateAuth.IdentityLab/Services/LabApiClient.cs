using Microsoft.JSInterop;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabApiClient(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return _module ??= await js.InvokeAsync<IJSObjectReference>(
            "import",
            "/js/lab-api.js");
    }

    public async Task<LabApiResponse<T>> GetAsync<T>(
        string url,
        CancellationToken ct = default)
    {
        var module = await GetModuleAsync();

        return await module.InvokeAsync<LabApiResponse<T>>(
            "request",
            ct,
            "GET",
            url,
            null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}

public sealed class LabApiResponse<T>
{
    public bool Ok { get; set; }
    public int Status { get; set; }
    public T? Body { get; set; }
}
