using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Provides utility methods for constructing URLs for UltimateAuth endpoints, taking into account multi-tenancy and transport options.
/// </summary>
public static class UAuthUrlBuilder
{
    /// <summary>
    /// Builds a complete URL for an UltimateAuth endpoint based on the provided authority, relative path, and multi-tenant options.
    /// </summary>
    /// <param name="authority"></param>
    /// <param name="relativePath"></param>
    /// <param name="tenant"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
