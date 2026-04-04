using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthMultiTenantOptionsValidator : IValidateOptions<UAuthMultiTenantOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthMultiTenantOptions options)
    {
        var errors = new List<string>();

        if (!options.Enabled)
        {
            if (options.EnableRoute || options.EnableHeader || options.EnableDomain)
            {
                errors.Add("Multi-tenancy is disabled, but one or more tenant resolvers are enabled. " +
                    "Either enable multi-tenancy or disable all tenant resolvers.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }

        if (!options.EnableRoute && !options.EnableHeader && !options.EnableDomain)
        {
            errors.Add("Multi-tenancy is enabled but no tenant resolver is active. " +
                "Enable at least one of: route, header or domain.");
        }

        if (options.EnableHeader)
        {
            if (string.IsNullOrWhiteSpace(options.HeaderName))
            {
                errors.Add("MultiTenant.HeaderName must be specified when header-based tenant resolution is enabled.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
