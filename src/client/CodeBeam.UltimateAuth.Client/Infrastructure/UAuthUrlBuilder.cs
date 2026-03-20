using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public static class UAuthUrlBuilder
{
    public static string Build(string authority, string relativePath, UAuthClientMultiTenantOptions tenant)
    {
        var baseAuthority = authority.TrimEnd('/');

        if (tenant.Enabled && tenant.Transport == TenantTransport.Route)
        {
            if (string.IsNullOrWhiteSpace(tenant.Tenant))
            {
                throw new InvalidOperationException("Tenant is enabled for route transport but no tenant value is provided.");
            }

            baseAuthority = "/" + tenant.Tenant.Trim('/') + baseAuthority;
        }

        return baseAuthority + "/" + relativePath.TrimStart('/');
    }
}
