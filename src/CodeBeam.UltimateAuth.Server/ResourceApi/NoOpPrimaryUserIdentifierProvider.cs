using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.ResourceApi;

internal class NoOpPrimaryUserIdentifierProvider : IPrimaryUserIdentifierProvider
{
    public Task<PrimaryUserIdentifiers?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
