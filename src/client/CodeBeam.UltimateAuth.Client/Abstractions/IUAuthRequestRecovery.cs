namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IUAuthRequestRecovery
{
    Task<UAuthTokenSnapshot> PrepareAsync(CancellationToken ct = default);

    Task<UAuthTokenSnapshot?> RecoverAsync(UAuthTokenSnapshot rejected, CancellationToken ct = default);
}
