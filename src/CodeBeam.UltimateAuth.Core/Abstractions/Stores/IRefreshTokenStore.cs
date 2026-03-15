using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IRefreshTokenStore
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    Task StoreAsync(RefreshToken token, CancellationToken ct = default);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);

    Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default);

    Task RevokeBySessionAsync(AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeByChainAsync(SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeAllForUserAsync(UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default);
}
