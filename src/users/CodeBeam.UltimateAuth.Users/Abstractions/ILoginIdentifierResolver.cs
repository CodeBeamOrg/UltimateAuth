using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users;

public interface ILoginIdentifierResolver
{
    Task<LoginIdentifierResolution?> ResolveAsync(TenantKey tenant, string identifier, CancellationToken ct = default);
}
