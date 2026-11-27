using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class UAuthRefreshTransport : IUAuthRefreshTransport
{
    private readonly UAuthRequestClient _raw;
    private readonly UAuthClientOptions _options;

    public UAuthRefreshTransport(UAuthRequestClient raw, IOptions<UAuthClientOptions> options)
    {
        _raw = raw;
        _options = options.Value;
    }

    public Task<UAuthTransportResult> SendAsync(CancellationToken ct = default)
    {
        var url = UAuthUrlBuilder.Build(_options.Endpoints.BasePath, _options.Endpoints.Refresh, _options.MultiTenant);
        return _raw.SendFormAsync(url, form: null, ct: ct);
    }
}
