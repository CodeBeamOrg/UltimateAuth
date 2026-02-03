using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

internal sealed class NoopAccessTokenIdStore : IAccessTokenIdStore
{
    public Task StoreAsync(TenantKey tenant, string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<bool> IsRevokedAsync(TenantKey tenant, string jti, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task RevokeAsync(TenantKey tenant, string jti, DateTimeOffset revokedAt, CancellationToken ct = default)
        => Task.CompletedTask;
}
