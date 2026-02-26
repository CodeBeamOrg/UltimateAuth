using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface ICustomLoginIdentifierResolver
{
    bool CanResolve(string identifier);

    Task<LoginIdentifierResolution?> ResolveAsync(TenantKey tenant, string identifier, CancellationToken ct = default);
}
