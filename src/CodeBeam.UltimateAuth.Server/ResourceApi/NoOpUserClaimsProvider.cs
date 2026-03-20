using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal sealed class NoOpUserClaimsProvider : IUserClaimsProvider
{
    public Task<IReadOnlyCollection<Claim>> GetClaimsAsync(TenantKey tenant, UserKey user, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyCollection<Claim>>(Array.Empty<Claim>());

    Task<ClaimsSnapshot> IUserClaimsProvider.GetClaimsAsync(TenantKey tenant, UserKey userKey, CancellationToken ct)
    {
        return Task.FromResult(ClaimsSnapshot.Empty);
    }
}
