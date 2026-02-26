using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public interface IUAuthRequestClient
{
    Task NavigateAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default);

    Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default);

    Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = null, CancellationToken ct = default);
}
