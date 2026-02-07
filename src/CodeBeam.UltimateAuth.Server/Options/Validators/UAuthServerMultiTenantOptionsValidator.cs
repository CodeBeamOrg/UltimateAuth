using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthServerMultiTenantOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();
        var multiTenant = options.MultiTenant;

        if (!multiTenant.Enabled)
        {
            if (multiTenant.EnableRoute || multiTenant.EnableHeader || multiTenant.EnableDomain)
            {
                errors.Add("Multi-tenancy is disabled, but one or more tenant resolvers are enabled. " +
                    "Either enable multi-tenancy or disable all tenant resolvers.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }

        if (!multiTenant.EnableRoute && !multiTenant.EnableHeader && !multiTenant.EnableDomain)
        {
            errors.Add("Multi-tenancy is enabled but no tenant resolver is active. " +
                "Enable at least one of: route, header or domain.");
        }

        if (multiTenant.EnableHeader)
        {
            if (string.IsNullOrWhiteSpace(multiTenant.HeaderName))
            {
                errors.Add("MultiTenant.HeaderName must be specified when header-based tenant resolution is enabled.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
