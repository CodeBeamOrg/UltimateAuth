using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    /// <summary>
    /// Validates <see cref="UAuthMultiTenantOptions"/> at application startup.
    /// Ensures that tenant configuration values (regex patterns, defaults,
    /// reserved identifiers, and requirement rules) are logically consistent
    /// and safe to use before multi-tenant authentication begins.
    /// </summary>
    internal sealed class UAuthMultiTenantOptionsValidator : IValidateOptions<UAuthMultiTenantOptions>
    {
        /// <summary>
        /// Performs validation on the provided <see cref="UAuthMultiTenantOptions"/> instance.
        /// This method enforces:
        /// - valid tenant id regex format,
        /// - reserved tenant ids matching the regex,
        /// - default tenant id consistency,
        /// - requirement rules coherence.
        /// </summary>
        /// <param name="name">Optional configuration section name.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>
        /// A <see cref="ValidateOptionsResult"/> indicating success or the
        /// specific configuration error encountered.
        /// </returns>
        public ValidateOptionsResult Validate(string? name, UAuthMultiTenantOptions options)
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
