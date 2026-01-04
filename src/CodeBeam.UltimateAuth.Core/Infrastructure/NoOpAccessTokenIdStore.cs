using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    internal sealed class NoopAccessTokenIdStore : IAccessTokenIdStore
    {
        public Task StoreAsync(string? tenantId, string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> IsRevokedAsync(string? tenantId, string jti, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task RevokeAsync(string? tenantId, string jti, DateTimeOffset revokedAt, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
