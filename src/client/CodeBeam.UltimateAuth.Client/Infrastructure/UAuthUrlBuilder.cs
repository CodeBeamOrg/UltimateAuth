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
        var endpoint = relativePath.TrimStart('/');

        if (!tenant.Enabled || tenant.Transport != TenantTransport.Route)
            return baseAuthority + "/" + endpoint;

        if (string.IsNullOrWhiteSpace(tenant.Tenant))
            throw new InvalidOperationException("Tenant is enabled for route transport but no tenant value is provided.");

        var tenantId = tenant.Tenant.Trim('/');

        if (tenantId.Length == 0 ||
            tenantId is "." or ".." ||
            tenantId.Contains('/') ||
            tenantId.Contains('\\') ||
            tenantId.Contains('?') ||
            tenantId.Contains('#'))
        {
            throw new ArgumentException("Invalid tenant route segment.", nameof(tenant));
        }

        var segment = Uri.EscapeDataString(tenantId);

        if (baseAuthority.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || baseAuthority.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        { 
            if (Uri.TryCreate(baseAuthority, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    throw new ArgumentException("Authority must use HTTP or HTTPS.", nameof(authority));

                if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                    throw new ArgumentException("Authority cannot contain a query or fragment.", nameof(authority));

                var origin = uri.GetLeftPart(UriPartial.Authority);
                var path = uri.AbsolutePath.TrimEnd('/');

                return origin + "/" + segment + path + "/" + endpoint;
            }
        }

        return "/" + segment + "/" + baseAuthority.TrimStart('/') + "/" + endpoint;
    }
}
