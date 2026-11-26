namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IUAuthResourceRequestClient
{
    Task<UAuthResourceTransportResult> SendAsync(string method, string endpoint, object? payload = null, CancellationToken ct = default);
}
