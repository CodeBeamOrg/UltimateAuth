using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthRecoveringRequestClient : IUAuthRequestClient
{
    private readonly UAuthRequestClient _raw;
    private readonly IUAuthRequestRecovery _recovery;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly UAuthClientAutoRefreshOptions _options;

    public UAuthRecoveringRequestClient(
        UAuthRequestClient raw,
        IUAuthRequestRecovery recovery,
        IUAuthTokenStore tokenStore,
        IOptions<UAuthClientOptions> options)
    {
        _raw = raw;
        _recovery = recovery;
        _tokenStore = tokenStore;
        _options = options.Value.AutoRefresh;
    }

    public Task NavigateAsync(
        string endpoint,
        IDictionary<string, string>? form = null,
        CancellationToken ct = default)
        => _raw.NavigateAsync(endpoint, form, ct);

    public Task<UAuthTransportResult> SendFormAsync(
        string endpoint,
        IDictionary<string, string>? form = null,
        CancellationToken ct = default)
        => _raw.SendFormAsync(endpoint, form, ct);

    public Task<UAuthTransportResult> SendFormAsync(
        string endpoint,
        IDictionary<string, string>? form,
        bool navigateOnSuccess,
        CancellationToken ct = default)
        => _raw.SendFormAsync(
            endpoint, form, navigateOnSuccess, ct);

    public Task<UAuthTransportResult> SendFormAsync(
        string endpoint,
        IDictionary<string, string>? form,
        string? accessToken,
        CancellationToken ct = default)
        => _raw.SendFormAsync(endpoint, form, accessToken, ct);

    public Task<UAuthTransportResult> SendJsonAsync(
        string endpoint,
        object? payload = null,
        CancellationToken ct = default)
        => _raw.SendJsonAsync(endpoint, payload, ct);


    public async Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload, UAuthRequestAuthentication authentication, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (authentication == UAuthRequestAuthentication.None)
            return await _raw.SendJsonAsync(endpoint, payload, ct);

        if (authentication is not UAuthRequestAuthentication.Recoverable)
        {
            throw new ArgumentOutOfRangeException(nameof(authentication));
        }

        var snapshot = await _recovery.PrepareAsync(ct);

        var first = await _raw.SendJsonRawAsync(endpoint, payload, snapshot, ct);

        if (!_options.Reactive || !first.CanRecoverAuthentication)
        {
            return first;
        }

        if (_tokenStore.Capture().Epoch != snapshot.Epoch)
            return first;

        var refreshed = await _recovery.RecoverAsync(snapshot, ct);

        if (refreshed is null)
            return first;

        ct.ThrowIfCancellationRequested();

        var current = _tokenStore.Capture();

        if (current.Epoch != snapshot.Epoch || current.Generation != refreshed.Generation)
        {
            return first;
        }

        return await _raw.SendJsonRawAsync(endpoint, payload, refreshed, ct);
    }


    public Task<TTryResult> TryAndCommitAsync<TTryResult>(
        string tryEndpoint,
        string commitEndpoint,
        object request,
        CancellationToken ct = default)
        => _raw.TryAndCommitAsync<TTryResult>(
            tryEndpoint, commitEndpoint, request, ct);

    public Task<UAuthLoginTransportInfo> GetLoginTransportAsync(
        string endpoint,
        CancellationToken ct = default)
        => _raw.GetLoginTransportAsync(endpoint, ct);

    public Task NavigateToUrlAsync(
        string url,
        CancellationToken ct = default)
        => _raw.NavigateToUrlAsync(url, ct);

    public Task NavigateToPkceReturnUrlAsync(
        string redirectUrl,
        string expectedReturnUrl,
        CancellationToken ct = default)
        => _raw.NavigateToPkceReturnUrlAsync(
            redirectUrl, expectedReturnUrl, ct);
}
