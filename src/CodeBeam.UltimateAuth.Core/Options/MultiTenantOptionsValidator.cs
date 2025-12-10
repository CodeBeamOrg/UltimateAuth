using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    /// <summary>
    /// Validates multi-tenant configuration before UltimateAuth services initialize.
    /// Prevents invalid tenant formats and misconfiguration issues.
    /// </summary>
    public sealed class MultiTenantOptionsValidator : IValidateOptions<MultiTenantOptions>
    {
        public ValidateOptionsResult Validate(string? name, MultiTenantOptions options)
        {
            // Multi-tenancy disabled → no validation needed
            if (!options.Enabled)
                return ValidateOptionsResult.Success;

            try
            {
                _ = new Regex(options.TenantIdRegex, RegexOptions.Compiled);
            }
            catch (Exception ex)
            {
                return ValidateOptionsResult.Fail(
                    $"Invalid TenantIdRegex '{options.TenantIdRegex}'. Regex error: {ex.Message}");
            }

            foreach (var reserved in options.ReservedTenantIds)
            {
                if (string.IsNullOrWhiteSpace(reserved))
                {
                    return ValidateOptionsResult.Fail(
                        "ReservedTenantIds cannot contain empty or whitespace values.");
                }

                if (!Regex.IsMatch(reserved, options.TenantIdRegex))
                {
                    return ValidateOptionsResult.Fail(
                        $"Reserved tenant id '{reserved}' does not match TenantIdRegex '{options.TenantIdRegex}'.");
                }
            }

            if (options.DefaultTenantId != null)
            {
                if (string.IsNullOrWhiteSpace(options.DefaultTenantId))
                {
                    return ValidateOptionsResult.Fail("DefaultTenantId cannot be empty or whitespace.");
                }

                if (!Regex.IsMatch(options.DefaultTenantId, options.TenantIdRegex))
                {
                    return ValidateOptionsResult.Fail($"DefaultTenantId '{options.DefaultTenantId}' does not match TenantIdRegex '{options.TenantIdRegex}'.");
                }

                if (options.ReservedTenantIds.Contains(options.DefaultTenantId))
                {
                    return ValidateOptionsResult.Fail($"DefaultTenantId '{options.DefaultTenantId}' is listed in ReservedTenantIds.");
                }
            }

            if (options.RequireTenant && options.DefaultTenantId == null)
            {
                return ValidateOptionsResult.Fail("RequireTenant = true, but DefaultTenantId is null. Provide a default tenant id or disable RequireTenant.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
