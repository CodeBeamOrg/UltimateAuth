using CodeBeam.UltimateAuth.Core;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthServerOptionsValidator
        : IValidateOptions<UAuthServerOptions>
    {
        public ValidateOptionsResult Validate(
            string? name,
            UAuthServerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.RoutePrefix))
            {
                return ValidateOptionsResult.Fail(
                    "RoutePrefix must be specified.");
            }

            if (!options.RoutePrefix.StartsWith("/"))
            {
                return ValidateOptionsResult.Fail(
                    "RoutePrefix must start with '/'.");
            }

            // -------------------------
            // AUTH MODE VALIDATION
            // -------------------------
            if (!Enum.IsDefined(typeof(UAuthMode), options.Mode))
            {
                return ValidateOptionsResult.Fail(
                    $"Invalid UAuthMode: {options.Mode}");
            }

            // -------------------------
            // SESSION VALIDATION
            // -------------------------
            if (options.Mode != UAuthMode.PureJwt)
            {
                if (options.Session.Lifetime <= TimeSpan.Zero)
                {
                    return ValidateOptionsResult.Fail(
                        "Session.Lifetime must be greater than zero.");
                }

                if (options.Session.MaxLifetime is not null &&
                    options.Session.MaxLifetime <= TimeSpan.Zero)
                {
                    return ValidateOptionsResult.Fail(
                        "Session.MaxLifetime must be greater than zero when specified.");
                }
            }

            // -------------------------
            // MULTI-TENANT VALIDATION
            // -------------------------
            if (options.MultiTenant.Enabled)
            {
                if (options.MultiTenant.RequireTenant &&
                    string.IsNullOrWhiteSpace(options.MultiTenant.DefaultTenantId))
                {
                    // This is allowed, but warn-worthy logic
                    // We still allow it, middleware will reject requests
                }

                if (string.IsNullOrWhiteSpace(options.MultiTenant.TenantIdRegex))
                {
                    return ValidateOptionsResult.Fail(
                        "MultiTenant.TenantIdRegex must be specified.");
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
