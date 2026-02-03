using Microsoft.Extensions.Options;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthMultiTenantOptionsValidator : IValidateOptions<UAuthMultiTenantOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthMultiTenantOptions options)
    {
        if (!options.Enabled)
        {
            if (options.RequireTenant)
            {
                return ValidateOptionsResult.Fail("RequireTenant cannot be true when multi-tenancy is disabled.");
            }

            return ValidateOptionsResult.Success;
        }

        if (!options.EnableRoute &&
            !options.EnableHeader &&
            !options.EnableDomain)
        {
            return ValidateOptionsResult.Fail(
                "Multi-tenancy is enabled but no tenant resolver is active " +
                "(route, header, or domain).");
        }

        return ValidateOptionsResult.Success;
    }
}
