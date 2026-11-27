namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>
/// Sends a refresh request through the raw client transport.
/// This operation must never trigger automatic authentication recovery.
/// </summary>
public interface IUAuthRefreshTransport
{
    Task<UAuthTransportResult> SendAsync(CancellationToken ct = default);
}
